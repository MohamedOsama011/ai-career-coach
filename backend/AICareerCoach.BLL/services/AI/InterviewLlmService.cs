using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

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

        public async Task<QuestionResult> GenerateNextQuestionAsync(
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
                        return new QuestionResult(text, UsedFallback: false);
                }
                catch (Exception ex) when (IsFatal(ex))
                {
                    _logger.LogError(ex, "Fatal error in question-gen (attempt {Attempt}): {Error}. Throwing.", attempt, ex.Message);
                    throw;
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

            return new QuestionResult(GetFallbackQuestion(track, targetRole), UsedFallback: true);
        }

        public async IAsyncEnumerable<StreamTokenDto> GenerateNextQuestionStreamAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson,
            List<InterviewMessage> transcript,
            int nextTurnNumber,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // The LLM streaming happens inside `ProduceStreamAsync` which has
            // full try/catch and retry logic. We bridge it to the IAsyncEnumerable
            // via an unbounded Channel so tokens reach the consumer AS THEY ARRIVE
            // (the previous buffered-list approach waited for the whole stream
            // to complete before yielding anything, which made the UI show
            // "AI is thinking" for the full generation time).
            var channel = Channel.CreateUnbounded<StreamTokenDto>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

            var producer = Task.Run(async () =>
            {
                try
                {
                    await ProduceStreamAsync(
                        track, difficulty, targetRole, summaryContextJson, transcript, nextTurnNumber,
                        channel.Writer, cancellationToken);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            try
            {
                await foreach (var token in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return token;
                }
            }
            finally
            {
                try { await producer; } catch { /* producer exceptions are surfaced via the channel */ }
            }
        }

        private async Task ProduceStreamAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson,
            List<InterviewMessage> transcript,
            int nextTurnNumber,
            ChannelWriter<StreamTokenDto> writer,
            CancellationToken cancellationToken)
        {
            var systemPrompt = BuildQuestionSystemPrompt(track, difficulty, targetRole, summaryContextJson);
            var messages = BuildTranscriptMessages(systemPrompt, transcript);

            Exception? lastError = null;
            Exception? attemptError = null;

            for (int attempt = 1; attempt <= MaxRetries + 1; attempt++)
            {
                var tokensEmittedThisAttempt = false;

                try
                {
                    _logger.LogInformation("Streaming question {Turn} — Attempt {Attempt}", nextTurnNumber, attempt);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                    await foreach (var update in _chatClient.CompleteChatStreamingAsync(
                        messages,
                        new ChatCompletionOptions
                        {
                            Temperature = 0.5f,
                            MaxOutputTokenCount = 500
                        },
                        cts.Token))
                    {
                        if (update.ContentUpdate is { Count: > 0 })
                        {
                            var token = string.Concat(update.ContentUpdate.Select(p => p.Text));
                            if (!string.IsNullOrEmpty(token))
                            {
                                tokensEmittedThisAttempt = true;
                                await writer.WriteAsync(
                                    new StreamTokenDto { Type = "token", Content = token },
                                    cancellationToken);
                            }
                        }
                    }

                    await writer.WriteAsync(new StreamTokenDto { Type = "done" }, cancellationToken);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (IsFatal(ex))
                {
                    _logger.LogError(ex, "Fatal error in streaming question-gen (attempt {Attempt}): {Error}.", attempt, ex.Message);
                    attemptError = ex;
                    break;
                }
                catch (Exception ex)
                {
                    attemptError = ex;

                    if (tokensEmittedThisAttempt)
                    {
                        _logger.LogWarning(ex, "Stream failed after some tokens on attempt {Attempt}; not retrying to avoid double-content.", attempt);
                        break;
                    }

                    _logger.LogWarning("Stream attempt {Attempt} failed before any tokens: {Error}. Retrying...", attempt, ex.Message);
                    lastError = ex;
                    try { await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); } catch { break; }
                }
            }

            if (lastError is not null)
                _logger.LogError(lastError, "Streaming question-gen failed after {MaxRetries} attempts; streaming fallback.", MaxRetries);
            else if (attemptError is not null)
                _logger.LogWarning("Streaming question-gen aborted on attempt; streaming fallback.");

            var fallback = GetFallbackQuestion(track, targetRole);
            await writer.WriteAsync(new StreamTokenDto { Type = "token", Content = fallback }, cancellationToken);
            await writer.WriteAsync(
                new StreamTokenDto { Type = "error", Code = "fallback", Message = "AI is temporarily unavailable; using a generic question." },
                cancellationToken);
            await writer.WriteAsync(new StreamTokenDto { Type = "done" }, cancellationToken);
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

        private static bool IsFatal(Exception ex)
        {
            if (ex is TaskCanceledException)
                return false;

            if (ex is ClientResultException cre)
            {
                // 4xx client errors except 429 (rate-limit) are fatal
                return cre.Status is >= 400 and < 500 and not 429;
            }

            if (ex is HttpRequestException hre && hre.StatusCode.HasValue)
            {
                return hre.StatusCode.Value is >= System.Net.HttpStatusCode.BadRequest and < System.Net.HttpStatusCode.InternalServerError
                       and not System.Net.HttpStatusCode.TooManyRequests;
            }

            return false;
        }

        public async Task<InterviewScorecardDto> GenerateScorecardAsync(
            List<InterviewMessage> transcript,
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string summaryContextJson)
        {
            var interviewerQuestionCount = transcript.Count(m => m.Role == MessageRole.Interviewer);
            var systemPrompt = BuildScorecardSystemPrompt(track, difficulty, targetRole, summaryContextJson, interviewerQuestionCount);
            string? lastFailureReason = null;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Generating scorecard — Attempt {Attempt}", attempt);

                    var prompt = attempt > 1
                        ? systemPrompt + BuildScorecardRetryCorrection(lastFailureReason, interviewerQuestionCount)
                        : systemPrompt;

                    var messages = BuildTranscriptMessages(prompt, transcript);

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

                    lastFailureReason = ValidateScorecard(result, transcript);
                    if (lastFailureReason is null)
                        return result;

                    _logger.LogWarning("Scorecard validation failed on attempt {Attempt} — reason: {Reason}.", attempt, lastFailureReason);

                    if (attempt < MaxRetries && TryNormalizeScorecard(result, transcript, out var normalized))
                    {
                        _logger.LogInformation("Scorecard normalized on retry.");
                        return normalized;
                    }
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

        /// <summary>
        /// Builds a targeted correction clause for the scorecard retry prompt
        /// based on the previous attempt's failure reason (Phase 7, M3).
        /// </summary>
        private static string BuildScorecardRetryCorrection(string? failureReason, int expectedQuestionCount)
        {
            return failureReason switch
            {
                "count" => $"\n\nIMPORTANT CORRECTION: The previous scorecard had the wrong number of questionAnalysis items. The transcript has exactly {expectedQuestionCount} question-answer pair(s). You MUST emit exactly {expectedQuestionCount} questionAnalysis items, one per pair, in transcript order.",
                "grade" => "\n\nIMPORTANT CORRECTION: letterGrade must be exactly one of: A, A-, B+, B, C. Do NOT use A+, B-, or other values.",
                "rating" => "\n\nIMPORTANT CORRECTION: rating must be exactly one of: Strong, Adequate, Weak. Do NOT use Excellent, Good, Poor, or other values.",
                "score" => "\n\nIMPORTANT CORRECTION: overallScore must be an integer between 0 and 100.",
                "content" => "\n\nIMPORTANT CORRECTION: Each questionAnalysis[].question and questionAnalysis[].answer must closely match the corresponding question and answer text from the transcript. Do NOT paraphrase, summarize, or fabricate.",
                _ => "\n\nIMPORTANT CORRECTION: The previous scorecard validation failed. letterGrade must be exactly one of: A, A-, B+, B, C. rating must be exactly one of: Strong, Adequate, Weak."
            };
        }

        /// <summary>
        /// Parses the session's <paramref name="summaryContextJson"/> snapshot
        /// and builds a structured "PERSONALIZED FOCUS AREAS" section for the
        /// question-gen / hint / scorecard prompts. Returns empty string when
        /// no focus-area data is present (graceful degradation — the interview
        /// works with CV-only context if no feedback/roadmap/jobs were available).
        /// </summary>
        private static string BuildFocusAreasSection(string summaryContextJson)
        {
            if (string.IsNullOrWhiteSpace(summaryContextJson))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(summaryContextJson);
                var root = doc.RootElement;

                var sb = new StringBuilder();

                // CV weaknesses
                if (root.TryGetProperty("cvWeaknesses", out var weaknesses) && weaknesses.ValueKind == JsonValueKind.Array)
                {
                    var items = weaknesses.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(5)
                        .ToList();
                    if (items.Count > 0)
                    {
                        sb.AppendLine("- CV-identified weaknesses:");
                        foreach (var w in items)
                            sb.AppendLine($"    • {w}");
                    }
                }

                // Missing keywords
                if (root.TryGetProperty("missingKeywords", out var keywords) && keywords.ValueKind == JsonValueKind.Array)
                {
                    var items = keywords.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(5)
                        .ToList();
                    if (items.Count > 0)
                    {
                        sb.AppendLine("- Missing CV keywords:");
                        sb.AppendLine($"    • {string.Join(", ", items)}");
                    }
                }

                // High-priority skill gaps
                if (root.TryGetProperty("highPriorityGaps", out var gaps) && gaps.ValueKind == JsonValueKind.Array)
                {
                    var gapItems = gaps.EnumerateArray()
                        .Where(e => e.TryGetProperty("skill", out _) )
                        .Take(5)
                        .ToList();
                    if (gapItems.Count > 0)
                    {
                        sb.AppendLine("- High-priority skill gaps (from roadmap):");
                        foreach (var g in gapItems)
                        {
                            var skill = g.GetProperty("skill").GetString() ?? "";
                            var current = g.TryGetProperty("currentLevel", out var cl) ? cl.GetString() ?? "" : "";
                            var required = g.TryGetProperty("requiredLevel", out var rl) ? rl.GetString() ?? "" : "";
                            sb.AppendLine($"    • {skill}: {current} → {required}");
                        }
                    }
                }

                // Seniority gap
                if (root.TryGetProperty("seniorityGap", out var sg) && sg.ValueKind == JsonValueKind.String)
                {
                    var gap = sg.GetString();
                    if (!string.IsNullOrWhiteSpace(gap))
                        sb.AppendLine($"- Seniority gap: {gap}");
                }

                // Job missing skills
                if (root.TryGetProperty("jobMissingSkills", out var jobSkills) && jobSkills.ValueKind == JsonValueKind.Array)
                {
                    var items = jobSkills.EnumerateArray()
                        .Select(e => e.GetString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(8)
                        .ToList();
                    if (items.Count > 0)
                    {
                        sb.AppendLine("- Skills required by target jobs (not yet in CV):");
                        sb.AppendLine($"    • {string.Join(", ", items)}");
                    }
                }

                var result = sb.ToString().Trim();
                return string.IsNullOrEmpty(result) ? string.Empty : result;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts the <c>cvExcerpt</c> field from the session's context JSON
        /// and truncates it to <paramref name="maxChars"/>. Returns empty string
        /// on any parse failure.
        /// </summary>
        private static string ExtractCvExcerptFromContext(string summaryContextJson, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(summaryContextJson))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(summaryContextJson);
                var excerpt = doc.RootElement.TryGetProperty("cvExcerpt", out var el) && el.ValueKind == JsonValueKind.String
                    ? el.GetString() ?? string.Empty
                    : string.Empty;
                return excerpt.Length > maxChars ? excerpt[..maxChars] : excerpt;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
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

            var cvExcerpt = ExtractCvExcerptFromContext(summaryContextJson, CvConstants.MaxLength);
            var focusSection = BuildFocusAreasSection(summaryContextJson);
            var focusBlock = string.IsNullOrEmpty(focusSection)
                ? string.Empty
                : $"""

                   PERSONALIZED FOCUS AREAS (derived from the candidate's CV feedback, skills-gap roadmap, and job recommendations):
                   {focusSection}

                   PRIORITIZE probing the candidate's identified gaps. Aim for 2-3 questions tied to these focus areas across the interview. Do NOT ask ONLY gap questions — mix with standard track questions. If a gap is listed, frame a question that tests that specific skill or area.
                   """;

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

                CANDIDATE CONTEXT:
                Target Role: {targetRole}
                CV Context (excerpt taken at session start):
                {cvExcerpt}{focusBlock}
                """;
        }

        private static string BuildScorecardSystemPrompt(
            InterviewTrack track, InterviewDifficulty difficulty, string targetRole, string summaryContextJson, int questionCount)
        {
            var cvExcerpt = ExtractCvExcerptFromContext(summaryContextJson, CvConstants.MaxLength);
            var focusSection = BuildFocusAreasSection(summaryContextJson);

            var cvSection = string.IsNullOrWhiteSpace(cvExcerpt)
                ? ""
                : $"""

                   CANDIDATE CV EXCERPT (for reference — assess whether the candidate accurately represented their stated experience):
                   {cvExcerpt}
                   """;

            var focusBlock = string.IsNullOrEmpty(focusSection)
                ? ""
                : $"""

                   IDENTIFIED FOCUS AREAS (from the candidate's CV feedback, skills-gap roadmap, and job recommendations):
                   {focusSection}

                   In overallSummary, comment on how the candidate performed on their identified focus areas relative to standard track questions. In areasForImprovement, reference any focus areas that remain weak.
                   """;

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

                The transcript contains exactly {{questionCount}} question-answer pair(s). Your questionAnalysis array MUST have exactly {{questionCount}} items.{{cvSection}}{{focusBlock}}
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

        public async Task<HintResponseDto> GenerateHintAsync(
            InterviewTrack track,
            InterviewDifficulty difficulty,
            string targetRole,
            string currentQuestion,
            string summaryContextJson)
        {
            var systemPrompt = BuildHintSystemPrompt(track, difficulty, targetRole, summaryContextJson);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Current interview question:\n{currentQuestion}")
            };

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Generating hint for {Track}/{Difficulty} — Attempt {Attempt}", track, difficulty, attempt);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                    var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                    {
                        Temperature = 0.6f,
                        MaxOutputTokenCount = 250
                    }, cts.Token);

                    var text = response.Value.Content[0].Text?.Trim();
                    if (!string.IsNullOrEmpty(text))
                        return new HintResponseDto { Hint = text };
                }
                catch (Exception ex) when (IsFatal(ex))
                {
                    _logger.LogError(ex, "Fatal error in hint generation (attempt {Attempt}): {Error}. Throwing.", attempt, ex.Message);
                    throw;
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning("Hint generation attempt {Attempt} failed: {Error}. Retrying...", attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hint generation failed after {MaxRetries} attempts.", MaxRetries);
                }
            }

            return new HintResponseDto
            {
                Hint = "Try breaking the problem into smaller parts and explaining your reasoning out loud."
            };
        }

        private static string BuildHintSystemPrompt(
            InterviewTrack track, InterviewDifficulty difficulty, string targetRole, string summaryContextJson)
        {
            var trackRubric = track switch
            {
                InterviewTrack.Behavioral => """
                    Track: Behavioral Interview
                    Focus: Leadership, teamwork, conflict resolution, STAR method, cultural fit.
                    """,
                InterviewTrack.TechnicalCoding => """
                    Track: Technical Coding Interview
                    Focus: Data structures, algorithms, problem-solving approach, time/space complexity.
                    """,
                InterviewTrack.SystemDesign => """
                    Track: System Design Interview
                    Focus: Architecture trade-offs, scalability, database design, distributed systems.
                    """,
                _ => ""
            };

            var focusSection = BuildFocusAreasSection(summaryContextJson);
            var focusBlock = string.IsNullOrEmpty(focusSection)
                ? string.Empty
                : $"""

                   CANDIDATE FOCUS AREAS (brief):
                   {focusSection}
                   """;

            return $"""
                You are a warm, encouraging career coach helping a candidate prepare for a {track} mock interview for a {targetRole} role at {difficulty} level.

                {trackRubric}

                The candidate is stuck on the current interview question. Provide a single, concise hint that:
                - Is encouraging and supportive in tone.
                - Nudges the candidate toward the right mental model or framework.
                - Does NOT give the full answer or a complete solution.
                - Is 1-2 sentences long, max 40 words.

                CANDIDATE CONTEXT:
                Target Role: {targetRole}{focusBlock}
                """;
        }

        private static InterviewScorecardDto ParseScorecardJson(string rawJson)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<InterviewScorecardDto>(rawJson, options)
                ?? throw new JsonException("Scorecard deserialized to null.");
        }

        /// <summary>
        /// Validates a scorecard against the transcript. Returns null on success
        /// or a short failure-reason code ("score"|"grade"|"rating"|"count"|"content")
        /// used to build a targeted retry correction clause (Phase 7, M2/M3).
        /// </summary>
        private static string? ValidateScorecard(InterviewScorecardDto dto, List<InterviewMessage> transcript)
        {
            if (dto.OverallScore < 0 || dto.OverallScore > 100)
                return "score";

            var validGrades = new[] { "A", "A-", "B+", "B", "C" };
            if (!validGrades.Contains(dto.LetterGrade))
                return "grade";

            var validRatings = new[] { "Strong", "Adequate", "Weak" };
            if (dto.QuestionAnalysis.Any(q => !validRatings.Contains(q.Rating)))
                return "rating";

            var interviewerQuestions = transcript
                .Where(m => m.Role == MessageRole.Interviewer)
                .Select(m => m.Content)
                .ToList();
            if (dto.QuestionAnalysis.Count != interviewerQuestions.Count)
                return "count";

            // M2: content validation — each analysis item's Question (and Answer)
            // must overlap the stored transcript text (Jaccard ≥ 0.5 on lowercased
            // word tokens). Rejects fabricated questions/answers while tolerating
            // minor LLM rephrasing.
            var candidateAnswers = transcript
                .Where(m => m.Role == MessageRole.Candidate)
                .Select(m => m.Content)
                .ToList();

            for (int i = 0; i < dto.QuestionAnalysis.Count; i++)
            {
                var qa = dto.QuestionAnalysis[i];
                if (TokenJaccard(qa.Question, interviewerQuestions[i]) < 0.5)
                    return "content";
                if (i < candidateAnswers.Count && TokenJaccard(qa.Answer, candidateAnswers[i]) < 0.5)
                    return "content";
            }

            return null;
        }

        /// <summary>
        /// Jaccard similarity on lowercased whitespace-delimited word-token sets.
        /// 1.0 = identical vocab, 0.0 = no shared words. Used by ValidateScorecard
        /// for content-matching (Phase 7, M2).
        /// </summary>
        private static double TokenJaccard(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return 0.0;

            var setA = new HashSet<string>(a.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            var setB = new HashSet<string>(b.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            if (setA.Count == 0 || setB.Count == 0)
                return 0.0;

            int intersection = 0;
            foreach (var token in setA)
                if (setB.Contains(token))
                    intersection++;

            int union = setA.Count + setB.Count - intersection;
            return union == 0 ? 0.0 : (double)intersection / union;
        }

        private static bool TryNormalizeScorecard(InterviewScorecardDto dto, List<InterviewMessage> transcript, out InterviewScorecardDto normalized)
        {
            normalized = dto;

            var questionCount = transcript.Count(m => m.Role == MessageRole.Interviewer);
            if (dto.QuestionAnalysis.Count != questionCount)
                return false;

            dto.LetterGrade = NormalizeGrade(dto.LetterGrade);
            if (dto.LetterGrade == null)
                return false;

            foreach (var qa in dto.QuestionAnalysis)
            {
                qa.Rating = NormalizeRating(qa.Rating);
                if (qa.Rating == null)
                    return false;
            }

            normalized = dto;
            return true;
        }

        private static string? NormalizeGrade(string grade)
        {
            var gradeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A+"] = "A",
                ["A"] = "A",
                ["A-"] = "A-",
                ["B+"] = "B+",
                ["B"] = "B",
                ["B-"] = "B",
                ["C+"] = "C",
                ["C"] = "C",
                ["C-"] = "C",
                ["D"] = "C",
                ["D+"] = "C",
                ["D-"] = "C",
                ["F"] = "C",
            };

            return gradeMap.TryGetValue(grade.Trim(), out var normalized) ? normalized : null;
        }

        private static string? NormalizeRating(string rating)
        {
            var ratingMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Excellent"] = "Strong",
                ["Exceptional"] = "Strong",
                ["Outstanding"] = "Strong",
                ["Strong"] = "Strong",
                ["Good"] = "Adequate",
                ["Average"] = "Adequate",
                ["Fair"] = "Adequate",
                ["Satisfactory"] = "Adequate",
                ["Adequate"] = "Adequate",
                ["Poor"] = "Weak",
                ["Weak"] = "Weak",
                ["Bad"] = "Weak",
                ["Insufficient"] = "Weak",
                ["Needs Improvement"] = "Weak",
            };

            return ratingMap.TryGetValue(rating.Trim(), out var normalized) ? normalized : null;
        }
    }
}
