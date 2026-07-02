using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services.AI
{
    public class SkillExtractionService : ISkillExtractionService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<SkillExtractionService> _logger;

        private const int BatchSize = 5;
        private const int MaxSkillsPerJob = 8;

        private static readonly string[] CommonTechKeywords = new[]
        {
            "C#", ".NET", ".NET Core", "ASP.NET", "ASP.NET Core", "Java", "Python", "JavaScript", "TypeScript",
            "Angular", "React", "Vue", "Node.js", "Node", "SQL Server", "PostgreSQL", "MySQL", "MongoDB",
            "Docker", "Kubernetes", "AWS", "Azure", "GCP", "Redis", "RabbitMQ", "Kafka",
            "Microservices", "REST APIs", "GraphQL", "Git", "CI/CD", "Terraform", "Linux",
            "Entity Framework", "LINQ", "xUnit", "Selenium", "T-SQL", "HTML5", "CSS3", "SCSS", "SASS",
            "RxJS", "TailwindCSS", "Bootstrap", "Flutter", "Dart", "Firebase", "Apache Spark",
            "Azure Data Factory", "Penetration Testing", "OWASP", "System Design", "Prompt Engineering",
            "OpenAI API", "Semantic Kernel", "LangChain"
        };

        public SkillExtractionService(IConfiguration config, ILogger<SkillExtractionService> logger)
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

        public async Task<Dictionary<string, List<string>>> ExtractSkillsBatchAsync(
            List<(string Id, string Title, string Description)> jobs,
            CancellationToken ct)
        {
            var result = new Dictionary<string, List<string>>();

            if (jobs.Count == 0) return result;

            for (int i = 0; i < jobs.Count; i += BatchSize)
            {
                var batch = jobs.Skip(i).Take(BatchSize).ToList();
                Dictionary<string, List<string>>? batchResult = null;

                try
                {
                    batchResult = await CallLlmForBatchAsync(batch, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM skill extraction failed for batch starting at index {Start}; using keyword fallback.", i);
                }

                foreach (var job in batch)
                {
                    List<string>? skills = null;
                    if (batchResult != null && batchResult.TryGetValue(job.Id, out var extracted) && extracted.Count > 0)
                    {
                        skills = extracted.Take(MaxSkillsPerJob).ToList();
                    }
                    else
                    {
                        skills = ExtractSkillsFallback(job.Title, job.Description);
                    }

                    result[job.Id] = skills;
                }
            }

            return result;
        }

        private async Task<Dictionary<string, List<string>>> CallLlmForBatchAsync(
            List<(string Id, string Title, string Description)> batch,
            CancellationToken ct)
        {
            var jsonTemplate = "{\"jobs\":[{\"id\":\"<external id>\",\"skills\":[\"skill1\",\"skill2\"]}]}";
            var systemPrompt =
                $"You are a technical recruiter. For each job, extract up to {MaxSkillsPerJob} required technical skills from the title and description." +
                $" Return JSON: {jsonTemplate}" +
                " Skills must be short (1-3 words), concrete technologies (e.g. 'C#', '.NET 8', 'SQL Server', 'React', 'Docker')." +
                " Exclude soft skills, generic terms ('teamwork'), and benefits." +
                " Do not include markdown formatting, no ```json tags, just raw JSON.";

            var jobLines = string.Join("\n\n", batch.Select((j, idx) =>
                $"Job {idx + 1} (id={j.Id}):\nTitle: {j.Title}\nDescription: {Truncate(j.Description, 600)}"));

            var userPrompt = $"Extract skills for these {batch.Count} jobs:\n\n{jobLines}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await _chatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    Temperature = 0.2f,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                },
                cts.Token);

            var rawJson = response.Value.Content[0].Text.Trim()
                .Replace("```json", "")
                .Replace("```", "");

            var parsed = JsonSerializer.Deserialize<SkillExtractionWrapper>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.Jobs == null) return new Dictionary<string, List<string>>();

            return parsed.Jobs
                .Where(j => !string.IsNullOrEmpty(j.Id))
                .ToDictionary(
                    j => j.Id,
                    j => (j.Skills ?? new List<string>())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList());
        }

        private static List<string> ExtractSkillsFallback(string title, string description)
        {
            var combined = $"{title} {description}";
            var found = new List<string>();
            foreach (var keyword in CommonTechKeywords)
            {
                if (combined.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(keyword);
                }
            }
            return found.Take(MaxSkillsPerJob).ToList();
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        private class SkillExtractionWrapper
        {
            [JsonPropertyName("jobs")]
            public List<SkillExtractionEntry> Jobs { get; set; } = new();
        }

        private class SkillExtractionEntry
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("skills")]
            public List<string>? Skills { get; set; }
        }
    }
}
