using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;


namespace AICareerCoach.BLL.Services.AI
{
    public class LlmExplanationService : ILlmExplanationService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<LlmExplanationService> _logger;

        public LlmExplanationService(IConfiguration config, ILogger<LlmExplanationService> logger)
        {
            _logger = logger;
            var apiKey = config["GitHub:Token"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("GitHub token not configured. Add 'GitHub:Token' to appsettings or user secrets.");

            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri("https://models.inference.ai.azure.com")
            };
            var credential = new ApiKeyCredential(apiKey);
            var openAiClient = new OpenAIClient(credential, options);
            _chatClient = openAiClient.GetChatClient("gpt-4o-mini");
        }
        public async Task<Dictionary<int, string>> GenerateExplanationsAsync(string cvText, List<Job> topJobs)
        {
            if (!topJobs.Any()) return new Dictionary<int, string>();

            var trimmedCv = cvText.Length > 3000 ? cvText[..3000] : cvText;

            var jobsContext = string.Join("\n\n", topJobs.Select(j =>
                $"Job ID: {j.Id}\nTitle: {j.Title}\nCompany: {j.Company}\nDescription: {j.Description}"));

            var systemPrompt = """
            You are an expert career advisor. Given a candidate's CV and a list of matched jobs,
            write ONE short, highly personalized sentence (max 20 words) per job explaining WHY it's a good match based on their experience.
            You must return ONLY a valid JSON object matching this structure:
            {
              "explanations": {
                "10": "Your experience with Angular aligns perfectly with their frontend stack.",
                "15": "Your leadership skills from your graduation project match the team lead role."
              }
            }
            Strict rules:
            - Substitute the keys with the actual Job IDs provided.
            - Do not include markdown formatting, no ```json tags, just raw JSON.
            - Keep explanations clear and in English.
            """;

            var userPrompt = $"Candidate CV:\n{trimmedCv}\n\nMatched Jobs List:\n{jobsContext}";

            try
            {
                var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<ExplanationWrapper>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return parsed?.Explanations?.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate match explanations via AI, activating fallback.");

                return topJobs.ToDictionary(j => j.Id, j => $"Your skills and profile align well with the requirements for {j.Title}.");
            }
        }
    }
    public class ExplanationWrapper
    {
        public Dictionary<string, string>? Explanations { get; set; }
    }
}
