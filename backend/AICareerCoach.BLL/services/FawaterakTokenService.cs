using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class FawaterakTokenService: IFawaterakTokenService
    {
        
    
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private string? _accessToken;
        private DateTime _expiresAt = DateTime.MinValue;

        public FawaterakTokenService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Return cached token if still valid
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                DateTime.UtcNow < _expiresAt)
            {
                return _accessToken;
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                _configuration["Fawaterak:TokenUrl"]);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _configuration["Fawaterak:ClientId"]! },
            { "client_secret", _configuration["Fawaterak:ClientSecret"]! }
        });

            var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get Fawaterak token: {json}");
            }

            var tokenResponse = JsonSerializer.Deserialize<FawaterakTokenResponseDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new Exception("Fawaterak returned an invalid access token.");
            }

            _accessToken = tokenResponse.AccessToken;

            // Refresh one minute before expiry
            _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

            return _accessToken;
        }
    }
}

