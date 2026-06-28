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

            var systemPrompt = $"""
                You are an expert technical mentor and career coach. Given a candidate's CV, a target role, and a reference roadmap template, your job is to personalize the template steps and perform a detailed skills gap analysis.

                [INPUT DATA]
                TARGET ROLE: {targetRole}
                REFERENCE TEMPLATE TRACK: {template.Track}
                REFERENCE TEMPLATE TITLE: {template.Title}
                REFERENCE TEMPLATE STEPS:
                {templateStepsText}

                [CRITICAL INSTRUCTIONS]
                1. TECH-STACK PRECEDENCE: The 'TARGET ROLE' takes absolute precedence over the 'REFERENCE TEMPLATE' tech-stack. If the user specifies a specific technology stack in the TARGET ROLE (e.g., MERN, React, Node.js) that differs from the template (e.g., .NET/Angular), you MUST dynamically override, replace, and pivot the languages, frameworks, and tools in the steps to match the requested target stack completely. Do NOT force .NET on a MERN request.
                2. STEP COUNT: Keep exactly the same number of steps as the template (7 steps), but rewrite them to form a logical progression (Beginner -> Advanced) for the requested TARGET ROLE.
                3. PERSONALIZATION: Tailor descriptions and resources to bridge the gap between the candidate's current CV and the target role.
                4. GAP ANALYSIS: Identify 5-10 specific skill gaps categorized into 2-3 categories (e.g., Technical Skills, Tools & Technologies). Ensure 'categoryName' and 'skillName' fields match the JSON schema.
                5. LEVEL ASSESSMENT: 'currentLevel' must be derived strictly from CV evidence (use "None" if completely missing). 'requiredLevel' is what the market demands for the TARGET ROLE.

                You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):
                {jsonStructure}

                All text must be in English.
                """;

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

        public async Task<List<SkillsCategoryDto>> GenerateGapAnalysisAsync(
            string cvText, string targetRole, Roadmap template)
        {
            var trimmedCv = cvText.Length > 3000 ? cvText[..3000] : cvText;

            var jsonStructure = """
            {
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

            var systemPrompt = $"""
                You are an expert technical mentor. Re-assess the candidate's skills gap against the target role, considering the reference template track. Return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):

                TARGET ROLE: {targetRole}
                TEMPLATE TRACK: {template.Track}
                TEMPLATE TITLE: {template.Title}

                {jsonStructure}

                All text must be in English.
                """;

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage($"CANDIDATE CV:\n{trimmedCv}")
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(
                    messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<RoadmapLlmResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return parsed?.GapAnalysis ?? GetFallback(template).GapAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rescan gap analysis, activating fallback.");
                return GetFallback(template).GapAnalysis;
            }
        }

        public async Task<List<RoadmapStepResultDto>> GenerateWeaknessStepsAsync(
            List<string> weakAreas, string cvText, string targetRole)
        {
            if (weakAreas == null || weakAreas.Count == 0)
                return new List<RoadmapStepResultDto>();

            var trimmedCv = cvText.Length > 2000 ? cvText[..2000] : cvText;

            var jsonStructure = """
            {
              "steps": [
                {
                  "order": 1,
                  "title": "Short actionable title (max 8 words)",
                  "description": "1-2 sentence explanation of what to do and why",
                  "level": "Beginner or Intermediate or Advanced",
                  "resources": ["https://example.com/relevant-resource"],
                  "duration": "1 week or 2 weeks etc."
                }
              ]
            }
            """;

            var weaknessList = string.Join("\n", weakAreas.Select((w, i) => $"{i + 1}. {w}"));

            var systemPrompt = $"""
                You are an expert technical mentor. For each weak area identified in a candidate's mock interview, produce ONE concrete, actionable roadmap step that the candidate can take to improve.

                You must return ONLY a valid JSON object matching this exact structure (no markdown, no ```json tags, no backticks):

                {jsonStructure}

                The 'steps' array must contain EXACTLY {weakAreas.Count} item(s) — one per weak area, in the same order.

                All text must be in English.
                """;

            var userPrompt = $"""
                TARGET ROLE: {targetRole}

                WEAK AREAS (one step per area):
                {weaknessList}

                CANDIDATE CV (for context, to tailor the advice):
                {trimmedCv}
                """;

            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _chatClient.CompleteChatAsync(
                    messages, new ChatCompletionOptions { Temperature = 0.3f }, cts.Token);

                var rawJson = response.Value.Content[0].Text.Trim()
                    .Replace("```json", "").Replace("```", "");

                var parsed = JsonSerializer.Deserialize<RoadmapLlmResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var steps = parsed?.Steps ?? new List<RoadmapStepResultDto>();

                if (steps.Count == 0)
                    return BuildFallbackSteps(weakAreas);

                if (steps.Count > weakAreas.Count)
                    steps = steps.Take(weakAreas.Count).ToList();

                for (int i = 0; i < steps.Count; i++)
                {
                    steps[i].Order = i + 1;
                }

                return steps;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate weakness steps via AI, activating fallback.");
                return BuildFallbackSteps(weakAreas);
            }
        }

        private static List<RoadmapStepResultDto> BuildFallbackSteps(List<string> weakAreas)
        {
            return weakAreas.Select((area, i) => new RoadmapStepResultDto
            {
                Order = i + 1,
                Title = Truncate(area, 60),
                Description = $"Work on improving: {area}. Review relevant learning resources and practice the concept.",
                Level = "Intermediate",
                Resources = new List<string>(),
                Duration = "2 weeks"
            }).ToList();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s[..max].TrimEnd() + "…";
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
