using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services.External
{
    public class JoobleJobProvider : IJobProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<JoobleJobProvider> _logger;

        public JoobleJobProvider(HttpClient httpClient, IConfiguration config, ILogger<JoobleJobProvider> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<List<JobFetchResultDto>> FetchJobsAsync(string country, int maxPages, CancellationToken ct)
        {
            var apiKey = _config["Jooble:ApiKey"];
            var baseUrl = _config["Jooble:BaseUrl"] ?? "https://jooble.org/api";
            var resultsPerPage = int.TryParse(_config["Jooble:ResultsPerPage"], out var rpp) ? rpp : 50;
            var keywords = _config["Jooble:Keywords"] ?? "developer OR engineer OR IT OR software";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Jooble API key missing. Skipping fetch for {Country}.", country);
                return new List<JobFetchResultDto>();
            }

            var allJobs = new List<JobFetchResultDto>();

            for (int page = 1; page <= maxPages; page++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var requestBody = new JoobleSearchRequest
                    {
                        Keywords = keywords,
                        Location = country,
                        Page = page
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"{baseUrl}/{apiKey}", content, ct);
                    response.EnsureSuccessStatusCode();

                    // Stream the response directly to the deserializer (avoids broken
                    // Content-Type header encoding issues — same pattern as Adzuna fix).
                    var stream = await response.Content.ReadAsStreamAsync(ct);
                    var result = await JsonSerializer.DeserializeAsync<JoobleSearchResponse>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        ct);

                    if (result?.Jobs == null || result.Jobs.Count == 0)
                    {
                        _logger.LogInformation("Jooble {Country} page {Page}: no results, stopping pagination.", country, page);
                        break;
                    }

                    var mapped = result.Jobs.Select(j => MapToDto(j, country)).ToList();
                    allJobs.AddRange(mapped);

                    _logger.LogInformation("Fetched {Count} jobs from Jooble {Country} page {Page}.", mapped.Count, country, page);

                    if (result.Jobs.Count < resultsPerPage)
                    {
                        break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error fetching Jooble jobs for {Country} page {Page}. Returning partial results.", country, page);
                    break;
                }
                catch (TaskCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error fetching Jooble jobs for {Country} page {Page}. Returning partial results.", country, page);
                    break;
                }
            }

            return allJobs;
        }

        private JobFetchResultDto MapToDto(JoobleJobResult j, string country)
        {
            // Jooble salary is a free-text string like "10000-15000 EGP" or "$50k-$70k".
            // Best-effort parse the first number; if no number, store 0 (frontend shows "Competitive").
            var (salaryMin, salaryMax) = ParseSalary(j.Salary);

            return new JobFetchResultDto
            {
                ExternalId = j.Id?.ToString() ?? string.Empty,
                Title = j.Title ?? string.Empty,
                Company = j.Company ?? string.Empty,
                Description = HtmlHelper.StripHtml(j.Snippet),
                Location = j.Location ?? string.Empty,
                SalaryMin = salaryMin,
                SalaryMax = salaryMax,
                RedirectUrl = j.Link,
                ContractType = j.Type,
                Category = j.Source,
                Created = j.Updated,
                Country = country,
                Latitude = null,
                Longitude = null,
                CompanyLogoUrl = BuildLogoUrl(j.Company)
            };
        }

        private string BuildLogoUrl(string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return null!;

            var apiKey = _config["LogoDev:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null!;

            var baseUrl = _config["LogoDev:BaseUrl"] ?? "https://img.logo.dev";

            // Heuristic: take the first word (most brand-identifying) + ".com".
            // "Vodafone Egypt" → "vodafone.com" | "AWS" → "aws.com" | "Cairo Bank" → "cairo.com".
            // If the guessed domain isn't registered, logo.dev returns a generic placeholder
            // (not a 404) so the frontend still shows a logo instead of falling back to initials.
            var firstWord = companyName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var slug = System.Text.RegularExpressions.Regex.Replace(firstWord.ToLowerInvariant(), @"[^a-z0-9]", "");

            if (string.IsNullOrEmpty(slug)) return null!;

            return $"{baseUrl}/{slug}.com?token={apiKey}&size=80";
        }

        private static (decimal Min, decimal Max) ParseSalary(string? salary)
        {
            if (string.IsNullOrWhiteSpace(salary)) return (0m, 0m);

            // Extract all digit groups from the string
            var matches = System.Text.RegularExpressions.Regex.Matches(salary, @"\d[\d,.]*");
            var numbers = new List<decimal>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var cleaned = match.Value.Replace(",", "").Replace(" ", "");
                if (decimal.TryParse(cleaned, out var num))
                {
                    numbers.Add(num);
                }
            }

            if (numbers.Count == 0) return (0m, 0m);
            if (numbers.Count == 1) return (numbers[0], numbers[0]);
            return (numbers[0], numbers[1]);
        }

        private class JoobleSearchRequest
        {
            [JsonPropertyName("keywords")]
            public string Keywords { get; set; } = string.Empty;

            [JsonPropertyName("location")]
            public string Location { get; set; } = string.Empty;

            [JsonPropertyName("page")]
            public int Page { get; set; } = 1;
        }

        private class JoobleSearchResponse
        {
            [JsonPropertyName("jobs")]
            public List<JoobleJobResult> Jobs { get; set; } = new();

            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }
        }

        private class JoobleJobResult
        {
            [JsonPropertyName("id")]
            public long? Id { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("company")]
            public string? Company { get; set; }

            [JsonPropertyName("location")]
            public string? Location { get; set; }

            [JsonPropertyName("snippet")]
            public string? Snippet { get; set; }

            [JsonPropertyName("salary")]
            public string? Salary { get; set; }

            [JsonPropertyName("link")]
            public string? Link { get; set; }

            [JsonPropertyName("updated")]
            public DateTime Updated { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("source")]
            public string? Source { get; set; }
        }
   }
}
