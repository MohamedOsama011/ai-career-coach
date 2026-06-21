using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.Services.AI
{
    public class RoadmapLlmService : IRoadmapLlmService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<RoadmapLlmService> _logger;

        public RoadmapLlmService(IConfiguration config, ILogger<RoadmapLlmService> logger)
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

        public async Task<(List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis)> GenerateRoadmapAsync(
            string cvText, string targetRole, Roadmap template)
        {
            var trimmedCv = cvText.Length > 3000 ? cvText[..3000] : cvText;

            var templateStepsText = string.Join("\n", template.Steps.OrderBy(s => s.OrderIndex).Select(s =>
                $"  Step {s.OrderIndex} [{s.Level}]: {s.Title} - {s.Description}"));

            var jsonStructure = """
            {
              "steps": [
                {
                  "order": 1,
                  "title": "Personalized step title",
                  "description": "Detailed description tailored to the candidate's current level",
                  "level": "Beginner or Intermediate or Advanced",
                  "resources": ["https://...", "https://..."],
                  "duration": "2 weeks or 1 month etc."
                }
              ],
              "gapAnalysis": [
                {
                  "category": "Technical Skills or Soft Skills or Tools & Technologies",
                  "skills": [
                    {
                      "skillName": "Skill name",
                      "currentLevel": "None or Beginner or Intermediate or Advanced",
                      "requiredLevel": "Beginner or Intermediate or Advanced",
                      "gap": "Explain what the candidate is missing",
                      "priority": "High or Medium or Low"
                    }
                  ]
                }
              ]
            }
            """;

            var systemPrompt = "You are an expert career coach. Given a candidate's CV, a target role, and a roadmap template, personalize the template steps and perform a detailed skills gap analysis.\n\n"
                + "TARGET ROLE: " + targetRole + "\n\n"
                + "TEMPLATE TRACK: " + template.Track + "\n"
                + "TEMPLATE TITLE: " + template.Title + "\n"
                + "TEMPLATE DESCRIPTION: " + template.Description + "\n"
                + "TEMPLATE STEPS:\n" + templateStepsText + "\n\n"
                + "You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags):\n"
                + jsonStructure + "\n\n"
                + "Rules:\n"
                + "- Keep the same number of steps as the template (7 steps) but adapt titles, descriptions, resources, and duration to the candidate.\n"
                + "- Gap analysis should identify 5-10 skill gaps across 2-3 categories.\n"
                + "- currentLevel comes from the CV evidence; requiredLevel from what the target role demands.\n"
                + "- Use \"None\" if the skill is absent from the CV entirely.\n"
                + "- Be honest and constructive in the gap description.\n"
                + "- All text must be in English.";

            var userPrompt = $"CANDIDATE CV:\n{trimmedCv}";

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<RoadmapLlmResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed?.Steps is null || parsed.GapAnalysis is null)
                    return GetFallback(template);

                return (parsed.Steps, parsed.GapAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate roadmap via AI, activating fallback.");
                return GetFallback(template);
            }
        }

        private static (List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis) GetFallback(Roadmap template)
        {
            var steps = template.Steps.OrderBy(s => s.OrderIndex).Select(s => new RoadmapStepResultDto
            {
                Order = s.OrderIndex,
                Title = s.Title,
                Description = s.Description,
                Level = s.Level,
                Resources = JsonSerializer.Deserialize<List<string>>(s.Resources) ?? new(),
                Duration = null
            }).ToList();

            var gapAnalysis = new List<SkillsCategoryDto>
            {
                new()
                {
                    Category = "Technical Skills",
                    Skills = new List<SkillGapItemDto>
                    {
                        new() { SkillName = template.Title, CurrentLevel = "Beginner", RequiredLevel = "Advanced", Gap = "Review the roadmap steps above and assess your current knowledge.", Priority = "High" }
                    }
                }
            };

            return (steps, gapAnalysis);
        }
    }

    internal class RoadmapLlmResponse
    {
        public List<RoadmapStepResultDto>? Steps { get; set; }
        public List<SkillsCategoryDto>? GapAnalysis { get; set; }
    }
}
