using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services.AI
{
    public class LlmService : ILlmService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<LlmService> _logger;

        private const string SystemPrompt = """
                You are a professional CV/Resume reviewer with 10+ years of experience in tech recruitment.
            Analyze the given CV text and return ONLY a valid JSON object with NO extra text,
            NO markdown, NO explanation — just raw JSON.

            Required JSON structure:
            {
              "overallScore": <integer 0-100, overall CV quality weight>,
              "keywordMatch": <integer 0-100, how well the CV matches job-relevant keywords and skills>,
              "impactStatements": <integer 0-100, quality of achievements and quantified results>,
              "formatting": <integer 0-100, layout, readability, consistent formatting>,
              "leadershipSignals": <integer 0-100, presence of management, mentoring, initiative examples>,
              "overallSummary": "<2-3 sentences about the CV overall>",
              "strengths": ["<strength 1>", "<strength 2>", "<strength 3>"],
              "missingKeywords": ["<keyword 1>", "<keyword 2>"],
              "suggestions": [
                {
                  "category": "<Format|Skills|Experience|Education|Summary>",
                  "issue": "<what is wrong>",
                  "recommendation": "<specific actionable fix>",
                  "priority": "<High|Medium|Low>",
                  "originalText": "<exact verbatim quote from the CV this suggestion applies to, or empty string>",
                  "suggestedText": "<the suggested replacement text, or empty string>"
                }
              ]
            }

            Rules:
            - suggestions array must have exactly 5 items
            - strengths array must have 3 items
            - missingKeywords should list tech skills/keywords the CV is missing
            - Be specific, not generic
            - Response must be valid parseable JSON only
            - All score fields (overallScore, keywordMatch, impactStatements, formatting, leadershipSignals) must be integers between 0 and 100.
            - Make sure 'overallScore' represents a fair weighted average of the detailed scores.
            - Each sub-score (keywordMatch, impactStatements, formatting, leadershipSignals) must be between 0 and 100.
            - Analyze the CV text carefully and assign realistic values for each sub-score.
            - For each suggestion, include 'originalText' (exact verbatim quote from the CV that the issue applies to) and 'suggestedText' (the replacement text) WHEN the issue applies to a specific part of the CV.
            - If the suggestion is general (e.g., 'add more keywords throughout'), leave both 'originalText' and 'suggestedText' as empty strings.
            - 'originalText' MUST be a verbatim quote that exists in the CV text (character-for-character). Do not paraphrase.
            - Limit 'originalText' to a single sentence or phrase (max 200 characters).
            """;

        public LlmService(IConfiguration config, ILogger<LlmService> logger)
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
        public async Task<CvFeedbackDto> GetCvFeedbackAsync(string cvText)
        {
            var trimmedCv = cvText.Length > CvConstants.MaxLength ? cvText[..CvConstants.MaxLength] : cvText;

            var messages = new List<ChatMessage>
            {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage($"Please analyze this CV:\n\n{trimmedCv}")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1500,
                Temperature = 0.2f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    _logger.LogInformation("Calling GitHub Models - Attempt {Attempt}", attempt);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); 
                    var response = await _chatClient.CompleteChatAsync(messages, options, cts.Token);

                    var rawJson = response.Value.Content[0].Text?.Trim()
                        ?? throw new Exception("Empty response from AI service.");

                    return ParseFeedbackJson(rawJson);
                }
                catch (Exception ex) when (attempt < 2)
                {
                    _logger.LogWarning("Attempt {Attempt} failed: {Error}. Retrying...", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2)); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI call totally failed after 2 attempts.");
                    return GetFallbackFeedback($"Analysis failed: {ex.Message}");
                }
            }

            return GetFallbackFeedback("Unexpected error.");
        }
        private static CvFeedbackDto ParseFeedbackJson(string rawJson)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<CvFeedbackDto>(rawJson, options)
                ?? throw new JsonException("Deserialized to null.");

            result.GeneratedAt = DateTime.UtcNow;
            return result;
        }

        private static CvFeedbackDto GetFallbackFeedback(string reason) => new()
        {
            OverallScore = 0,
            OverallSummary = "We couldn't analyze your CV at this moment. Please try again later.",
            Strengths = new() { "System temporary error" },
            MissingKeywords = new(),
            Suggestions = new() { new() { Category = "System", Issue = reason, Recommendation = "Try again later", Priority = "High", OriginalText = string.Empty, SuggestedText = string.Empty } },
            FromCache = false,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
