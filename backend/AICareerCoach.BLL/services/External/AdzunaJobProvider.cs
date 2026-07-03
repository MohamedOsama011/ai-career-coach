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
    public class AdzunaJobProvider : IJobProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AdzunaJobProvider> _logger;

        public AdzunaJobProvider(HttpClient httpClient, IConfiguration config, ILogger<AdzunaJobProvider> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<List<JobFetchResultDto>> FetchJobsAsync(string country, int maxPages, CancellationToken ct)
        {
            var appId = _config["Adzuna:AppId"];
            var appKey = _config["Adzuna:AppKey"];
            var baseUrl = _config["Adzuna:BaseUrl"] ?? "https://api.adzuna.com/v1/api";
            var resultsPerPage = int.TryParse(_config["Adzuna:ResultsPerPage"], out var rpp) ? rpp : 50;
            var category = _config["Adzuna:Category"] ?? "it-jobs";

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
            {
                _logger.LogWarning("Adzuna credentials missing. Skipping fetch for country {Country}.", country);
                return new List<JobFetchResultDto>();
            }

            var allJobs = new List<JobFetchResultDto>();

            for (int page = 1; page <= maxPages; page++)
            {
                ct.ThrowIfCancellationRequested();

                var url = $"{baseUrl}/jobs/{country}/search/{page}" +
                          $"?app_id={Uri.EscapeDataString(appId)}" +
                          $"&app_key={Uri.EscapeDataString(appKey)}" +
                          $"&results_per_page={resultsPerPage}" +
                          $"&category={Uri.EscapeDataString(category)}";

                try
                {
                    var httpResponse = await _httpClient.GetAsync(url, ct);
                    httpResponse.EnsureSuccessStatusCode();

                    // Stream the response directly into the deserializer — bypasses the broken
                    // `charset=utf8` Content-Type header that Adzuna returns (non-standard
                    // encoding name; .NET's ReadAsStringAsync throws on it).
                    var response = await JsonSerializer.DeserializeAsync<AdzunaSearchResponse>(
                        await httpResponse.Content.ReadAsStreamAsync(ct),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        ct);

                    if (response?.Results == null || response.Results.Count == 0)
                    {
                        _logger.LogInformation("Adzuna {Country} page {Page}: no results, stopping pagination.", country, page);
                        break;
                    }

                    var mapped = response.Results.Select(r => MapToDto(r, country)).ToList();
                    allJobs.AddRange(mapped);

                    _logger.LogInformation("Fetched {Count} jobs from Adzuna {Country} page {Page}.", mapped.Count, country, page);

                    if (response.Results.Count < resultsPerPage)
                    {
                        break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error fetching Adzuna jobs for {Country} page {Page}. Returning partial results.", country, page);
                    break;
                }
                catch (TaskCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error fetching Adzuna jobs for {Country} page {Page}. Returning partial results.", country, page);
                    break;
                }
            }

            return allJobs;
        }

        private JobFetchResultDto MapToDto(AdzunaJobResult r, string country)
        {
            return new JobFetchResultDto
            {
                ExternalId = r.Id ?? string.Empty,
                Title = r.Title ?? string.Empty,
                Company = r.Company?.Display_name ?? string.Empty,
                Description = HtmlHelper.StripHtml(r.Description),
                Location = r.Location?.Display_name ?? string.Empty,
                SalaryMin = r.SalaryMin ?? 0m,
                SalaryMax = r.SalaryMax ?? 0m,
                RedirectUrl = r.RedirectUrl,
                ContractType = r.ContractType ?? r.ContractTime,
                Category = r.Category?.Tag ?? r.Category?.Label,
                Created = r.Created,
                Country = country,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                CompanyLogoUrl = BuildLogoUrl(r.Company?.Display_name)
            };
        }

        private string BuildLogoUrl(string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return null!;

            var apiKey = _config["LogoDev:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null!;

            var baseUrl = _config["LogoDev:BaseUrl"] ?? "https://img.logo.dev";

            var firstWord = companyName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var slug = System.Text.RegularExpressions.Regex.Replace(firstWord.ToLowerInvariant(), @"[^a-z0-9]", "");

            if (string.IsNullOrEmpty(slug)) return null!;

            return $"{baseUrl}/{slug}.com?token={apiKey}&size=80";
        }

        private class AdzunaSearchResponse
        {
            [JsonPropertyName("results")]
            public List<AdzunaJobResult> Results { get; set; } = new();

            [JsonPropertyName("count")]
            public int Count { get; set; }
        }

        private class AdzunaJobResult
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("company")]
            public AdzunaCompany? Company { get; set; }

            [JsonPropertyName("location")]
            public AdzunaLocation? Location { get; set; }

            [JsonPropertyName("salary_min")]
            public decimal? SalaryMin { get; set; }

            [JsonPropertyName("salary_max")]
            public decimal? SalaryMax { get; set; }

            [JsonPropertyName("redirect_url")]
            public string? RedirectUrl { get; set; }

            [JsonPropertyName("contract_type")]
            public string? ContractType { get; set; }

            [JsonPropertyName("contract_time")]
            public string? ContractTime { get; set; }

            [JsonPropertyName("category")]
            public AdzunaCategory? Category { get; set; }

            [JsonPropertyName("created")]
            public DateTime Created { get; set; }

            [JsonPropertyName("latitude")]
            public double? Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double? Longitude { get; set; }
        }

        private class AdzunaCompany
        {
            [JsonPropertyName("display_name")]
            public string? Display_name { get; set; }
        }

        private class AdzunaLocation
        {
            [JsonPropertyName("display_name")]
            public string? Display_name { get; set; }

            [JsonPropertyName("area")]
            public List<string> Area { get; set; } = new();
        }

        private class AdzunaCategory
        {
            [JsonPropertyName("label")]
            public string? Label { get; set; }

            [JsonPropertyName("tag")]
            public string? Tag { get; set; }
        }
    }
}
