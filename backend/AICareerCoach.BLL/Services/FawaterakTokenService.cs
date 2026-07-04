using System.Text.Json;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class FawaterakTokenService : IFawaterakTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FawaterakTokenService> _logger;
        private const string CacheKey = "FawaterakAccessToken";

        public FawaterakTokenService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<FawaterakTokenService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_cache.TryGetValue(CacheKey, out string? cachedToken) && cachedToken != null)
                return cachedToken;

            var clientId = _configuration["Fawaterak:ClientId"]
                ?? throw new InvalidOperationException("Fawaterak:ClientId is not configured.");
            var clientSecret = _configuration["Fawaterak:ClientSecret"]
                ?? throw new InvalidOperationException("Fawaterak:ClientSecret is not configured.");
            var tokenUrl = _configuration["Fawaterak:TokenUrl"]
                ?? throw new InvalidOperationException("Fawaterak:TokenUrl is not configured.");

            var requestBody = new FawaterakTokenRequestDto
            {
                grant_type = "client_credentials",
                client_id = clientId,
                client_secret = clientSecret,
                scope = "all",
            };

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fawaterak token request failed: {Result}", result);
                throw new Exception("Failed to obtain Fawaterak access token.");
            }

            var tokenResponse = JsonSerializer.Deserialize<FawaterakTokenResponseDto>(
                result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                throw new Exception("Invalid Fawaterak token response.");

            var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;
            _cache.Set(CacheKey, tokenResponse.AccessToken, TimeSpan.FromSeconds(expiresIn - 60));

            _logger.LogInformation("Fawaterak access token obtained, expires in {ExpiresIn}s", expiresIn);

            return tokenResponse.AccessToken;
        }
    }
}
