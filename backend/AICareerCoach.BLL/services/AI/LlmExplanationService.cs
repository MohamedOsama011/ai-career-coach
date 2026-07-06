using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


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

        public async Task<Dictionary<int, JobExplanationDto>> GenerateExplanationsAsync(string cvText, List<Job> topJobs)
        {
            if (!topJobs.Any()) return new Dictionary<int, JobExplanationDto>();

            var trimmedCv = cvText.Length > CvConstants.MaxLength ? cvText[..CvConstants.MaxLength] : cvText;

            var jobsContext = string.Join("\n\n", topJobs.Select(j =>
                $"Job ID: {j.Id}\nTitle: {j.Title}\nCompany: {j.Company}\nDescription: {j.Description}\nRequired Skills: {j.RequiredSkills}"));

            var systemPrompt = """
            You are an expert career advisor. Given a candidate's CV and a list of matched jobs, produce TWO things per job in a single response:
            1. A short, highly personalized sentence (max 20 words) explaining WHY it's a good match based on their experience.
            2. A list of up to 5 specific skills the candidate is missing that the job requires (drawn from the job's required skills). If the candidate clearly has the required skills, return an empty list. Only include skills the candidate does NOT already demonstrate.

            You must return ONLY a valid JSON object matching this structure:
            {
              "explanations": {
                "10": {
                  "explanation": "Your experience with Angular aligns perfectly with their frontend stack.",
                  "missingSkills": ["Kubernetes", "TypeScript", "AWS Lambda"]
                }
              }
            }

            Strict rules:
            - Substitute the keys with the actual Job IDs provided.
            - missingSkills must be drawn from the job's required skills (or closely related); never invent unrelated skills.
            - Limit missingSkills to at most 5 items per job. If the candidate matches well, use [].
            - Do not include markdown formatting, no ```json tags, just raw JSON.
            - Keep all text in English.
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

                if (parsed?.Explanations == null)
                    return topJobs.ToDictionary(j => j.Id, _ => new JobExplanationDto());

                return parsed.Explanations.ToDictionary(
                    kv => int.Parse(kv.Key),
                    kv => new JobExplanationDto
                    {
                        Explanation = kv.Value.Explanation ?? string.Empty,
                        MissingSkills = kv.Value.MissingSkills ?? new List<string>()
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate match explanations via AI, activating fallback.");

                return topJobs.ToDictionary(j => j.Id, j => new JobExplanationDto
                {
                    Explanation = $"Your skills and profile align well with the requirements for {j.Title}.",
                    MissingSkills = new List<string>()
                });
            }
        }
    }

    public class ExplanationWrapper
    {
        public Dictionary<string, ExplanationEntry>? Explanations { get; set; }
    }

    public class ExplanationEntry
    {
        public string? Explanation { get; set; }
        public List<string>? MissingSkills { get; set; }
    }
}
