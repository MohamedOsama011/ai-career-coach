using AICareerCoach.BLL.DTOs.Interview;
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
    public class InterviewLlmService : IInterviewLlmService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<InterviewLlmService> _logger;

        private const int MaxRetries = 2;
        private const int TimeoutSeconds = 30;

        public InterviewLlmService(IConfiguration config, ILogger<InterviewLlmService> logger)
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

        public async Task<string> GenerateNextQuestionAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson,
            List<InterviewMessage> transcript,
            int nextTurnNumber)
        {
            var systemPrompt = BuildQuestionSystemPrompt(track, difficulty, targetRole, summaryContextJson);
            var messages = BuildTranscriptMessages(systemPrompt, transcript);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Generating question {Turn} — Attempt {Attempt}", nextTurnNumber, attempt);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                    var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                    {
                        Temperature = 0.5f,
                        MaxOutputTokenCount = 500
                    }, cts.Token);

                    var text = response.Value.Content[0].Text?.Trim();
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning("Question-gen attempt {Attempt} failed: {Error}. Retrying...", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Question-gen failed after {MaxRetries} attempts.", MaxRetries);
                }
            }

            return GetFallbackQuestion(track, targetRole);
        }

        public async Task<InterviewScorecardDto> GenerateScorecardAsync(
            List<InterviewMessage> transcript,
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole)
        {
            var systemPrompt = BuildScorecardSystemPrompt(track, difficulty, targetRole);
            var messages = BuildTranscriptMessages(systemPrompt, transcript);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Generating scorecard — Attempt {Attempt}", attempt);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                    var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                    {
                        Temperature = 0.2f,
                        MaxOutputTokenCount = 2000,
                        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                    }, cts.Token);

                    var rawJson = response.Value.Content[0].Text?.Trim()
                        ?? throw new Exception("Empty response from AI service.");

                    var result = ParseScorecardJson(rawJson);

                    if (!ValidateScorecard(result, transcript))
                    {
                        _logger.LogWarning("Scorecard validation failed on attempt {Attempt}.", attempt);
                        continue;
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning("Scorecard attempt {Attempt} failed: {Error}. Retrying...", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scorecard generation failed after {MaxRetries} attempts.", MaxRetries);
                    throw;
                }
            }

            throw new InvalidOperationException("Scorecard generation failed after validation retries.");
        }

        private static string BuildQuestionSystemPrompt(
            InterviewTrack track, InterviewDifficulty difficulty, string targetRole, string summaryContextJson)
        {
            var trackRubric = track switch
            {
                InterviewTrack.Behavioral => """
                    Track: Behavioral Interview
                    Focus: Leadership, teamwork, conflict resolution, STAR method, cultural fit, growth mindset.
                    """,
                InterviewTrack.TechnicalCoding => """
                    Track: Technical Coding Interview
                    Focus: Data structures, algorithms, problem-solving approach, code quality, time/space complexity.
                    """,
                InterviewTrack.SystemDesign => """
                    Track: System Design Interview
                    Focus: Architecture trade-offs, scalability, database design, API design, distributed systems.
                    """,
                _ => ""
            };

            var calibration = difficulty switch
            {
                InterviewDifficulty.Junior => "Calibration: Junior-level. Expect foundational concepts. Be encouraging and ask follow-ups to probe depth when answers are shallow. Avoid advanced/distributed-systems topics.",
                InterviewDifficulty.MidLevel => "Calibration: Mid-level. Expect solid fundamentals and practical experience. Ask about trade-offs and real-world scenarios. Probe for depth and edge cases.",
                InterviewDifficulty.Senior => "Calibration: Senior-level. Expect deep expertise and system-level thinking. Push for trade-off analysis, scalability, leadership patterns, and mentoring approaches.",
                _ => ""
            };

            return $"""
                You are a professional technical interviewer conducting a mock interview. Your role is to ask one question at a time, listen to the candidate's answer, and then ask a relevant follow-up or move to a new topic.

                {trackRubric}

                {calibration}

                RULES:
                - Ask ONE question per turn.
                - Do NOT answer your own questions.
                - If the candidate's previous answer was weak, ask a simpler follow-up on the same topic to probe depth.
                - If the candidate's previous answer was strong, acknowledge briefly ("Got it.") and move to a new topic.
                - Keep questions concise (1-3 sentences max).
                - Be professional but conversational.
                - Do NOT repeat previously asked questions.

                CANDIDATE CONTEXT (target role + CV excerpt):
                Target Role: {targetRole}
                CV Context (4,000-char excerpt taken at session start):
                {summaryContextJson}
                """;
        }

        private static string BuildScorecardSystemPrompt(
            InterviewTrack track, InterviewDifficulty difficulty, string targetRole)
        {
            return $$"""
                You are an expert interview evaluator. Score the candidate's performance in this {{track}} mock interview for a {{targetRole}} position at {{difficulty}} level.

                Analyze each question-answer pair independently and provide an overall assessment.

                Return ONLY a valid JSON object matching this exact structure (no markdown, no backticks):
                {
                  "overallScore": <integer 0-100>,
                  "letterGrade": "<A | A- | B+ | B | C>",
                  "overallSummary": "<2-3 sentence summary of performance>",
                  "strengths": ["<strength 1>", "<strength 2>", "<strength 3>"],
                  "areasForImprovement": ["<area 1>", "<area 2>", "<area 3>"],
                  "questionAnalysis": [
                    {
                      "question": "<exact question text from transcript>",
                      "answer": "<exact answer text from transcript>",
                      "rating": "<Strong | Adequate | Weak>",
                      "explanation": "<brief explanation of the rating>",
                      "improvementSuggestion": "<specific actionable suggestion>"
                    }
                  ]
                }

                Grading scale:
                - A (90-100): Exceptional — deep knowledge, clear communication, strong problem-solving
                - A- (85-89): Excellent — minor gaps, strong overall
                - B+ (80-84): Very Good — solid foundation, some room to grow
                - B (70-79): Good — adequate but needs improvement in several areas
                - C (<70): Needs significant improvement

                Ensure questionAnalysis has exactly the same number of items as question-answer pairs in the transcript.
                """;
        }

        private static List<ChatMessage> BuildTranscriptMessages(string systemPrompt, List<InterviewMessage> transcript)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            foreach (var msg in transcript)
            {
                if (msg.Role == MessageRole.Interviewer)
                    messages.Add(new AssistantChatMessage(msg.Content));
                else
                    messages.Add(new UserChatMessage(msg.Content));
            }

            return messages;
        }

        private static InterviewScorecardDto ParseScorecardJson(string rawJson)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<InterviewScorecardDto>(rawJson, options)
                ?? throw new JsonException("Scorecard deserialized to null.");
        }

        private static bool ValidateScorecard(InterviewScorecardDto dto, List<InterviewMessage> transcript)
        {
            if (dto.OverallScore < 0 || dto.OverallScore > 100)
                return false;

            var validGrades = new[] { "A", "A-", "B+", "B", "C" };
            if (!validGrades.Contains(dto.LetterGrade))
                return false;

            var validRatings = new[] { "Strong", "Adequate", "Weak" };
            if (dto.QuestionAnalysis.Any(q => !validRatings.Contains(q.Rating)))
                return false;

            var questionCount = transcript.Count(m => m.Role == MessageRole.Interviewer);
            if (dto.QuestionAnalysis.Count != questionCount)
                return false;

            return true;
        }

        private static string GetFallbackQuestion(InterviewTrack track, string targetRole)
        {
            return track switch
            {
                InterviewTrack.Behavioral => $"Tell me about a time you faced a challenging situation working with a teammate. How did you handle it?",
                InterviewTrack.TechnicalCoding => $"Can you explain the difference between an array and a linked list, and when you would use each?",
                InterviewTrack.SystemDesign => $"How would you design a URL shortening service like TinyURL? Walk through the key components and trade-offs.",
                _ => $"Tell me about your experience with {targetRole} and why you're interested in this role."
            };
        }
    }
}
