using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache cache;

        

        public FawaterakTokenService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            cache = memoryCache;
        }

        public async Task<string> GetAccessTokenAsync()
        
        {

            //private const string CacheKey = "FawaterakToken";
            
            if (cache.TryGetValue("Fawateraktoken", out string token))
            {
                return token;

            }
            var request = new HttpRequestMessage(
                HttpMethod.Post,
               $"{ _configuration["Fawaterak:tokenurl"] }");

            var requestBody = new FwateraktokenrequestDTO
            {
                grant_type = "client_credentials",
                client_id = _configuration["Fawaterak:clientid"]!,
                client_secret = _configuration["Fawaterak:clientsecret"]!,
                scope = ""
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8,"application/json"
                );
        

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
            if (tokenResponse.ExpiresIn > 60)
            {
                cache.Set("Fawateraktoken",
                          tokenResponse.AccessToken,
                          TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60));
            }
            else
            {
                cache.Set("FawaterakToken",
                          tokenResponse.AccessToken,
                          TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
            }


            return tokenResponse.AccessToken; ;

           
        }
    }
}

