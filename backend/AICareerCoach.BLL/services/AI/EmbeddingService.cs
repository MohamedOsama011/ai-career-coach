using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;


namespace AICareerCoach.BLL.Services.AI
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly EmbeddingClient _embeddingClient;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger)
        {
            _logger = logger;

            var apiKey = config["GitHub:Token"]
                ?? throw new InvalidOperationException("GitHub token is not configured.");

            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            };
            var credential = new ApiKeyCredential(apiKey);
            var openAiClient = new OpenAIClient(credential, options);
            _embeddingClient = openAiClient.GetEmbeddingClient("text-embedding-3-small");
        }
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<float>();

            var trimmedText = text.Length > CvConstants.MaxLength ? text[..CvConstants.MaxLength] : text;

            try
            {
                var response = await _embeddingClient.GenerateEmbeddingAsync(trimmedText);

                return response.Value.ToFloats().ToArray();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw new Exception("Failed to process text understanding, please try again.", ex);
            }

        }
    }
}
