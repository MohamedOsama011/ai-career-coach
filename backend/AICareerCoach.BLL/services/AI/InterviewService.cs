using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Exceptions;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AICareerCoach.BLL.Services.AI
{
    public class InterviewService : IInterviewService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IInterviewLlmService _llmService;
        private readonly IRoadmapLlmService _roadmapLlmService;
        private readonly IUserRoadmapService _userRoadmapService;
        private readonly ICvFeedbackService _cvFeedbackService;
        private readonly IJobRecommendationService _jobRecommendationService;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(
            AICareerCoachDbContext context,
            IInterviewLlmService llmService,
            IRoadmapLlmService roadmapLlmService,
            IUserRoadmapService userRoadmapService,
            ICvFeedbackService cvFeedbackService,
            IJobRecommendationService jobRecommendationService,
            ILogger<InterviewService> logger)
        {
            _context = context;
            _llmService = llmService;
            _roadmapLlmService = roadmapLlmService;
            _userRoadmapService = userRoadmapService;
            _cvFeedbackService = cvFeedbackService;
            _jobRecommendationService = jobRecommendationService;
            _logger = logger;
        }

        public async Task<InterviewOptionsDto> GetOptionsAsync(string userId)
        {
            var tracks = Enum.GetValues<InterviewTrack>()
                .Select(v => new InterviewOptionItem
                {
                    Value = v.ToString(),
                    Label = EnumDisplay.Name(v)
                })
                .ToList();

            var difficulties = Enum.GetValues<InterviewDifficulty>()
                .Select(v => new InterviewOptionItem
                {
                    Value = v.ToString(),
                    Label = EnumDisplay.Name(v)
                })
                .ToList();

            var focusContext = await GatherFocusContextAsync(userId);

            return new InterviewOptionsDto
            {
                Tracks = tracks,
                Difficulties = difficulties,
                FocusAreas = focusContext.FocusAreas
            };
        }

        public async Task<InterviewSessionDto> StartSessionAsync(string userId, StartSessionRequestDto request)
        {
            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            if (cv is null)
            {
                _logger.LogWarning("StartSession blocked: no CV found for user {UserId}.", userId);
                throw new KeyNotFoundException("Please upload your CV first to start a mock interview.");
            }

            if (string.IsNullOrEmpty(cv.ExtractedData))
            {
                _logger.LogWarning("StartSession blocked: CV {CvId} has no ExtractedData for user {UserId}.", cv.CVId, userId);
                throw new InvalidOperationException("CV text not extracted yet. Please request CV feedback first.");
            }

            var track = ParseEnum<InterviewTrack>(request.Track);
            var difficulty = ParseEnum<InterviewDifficulty>(request.Difficulty);

            var cvExcerpt = cv.ExtractedData.Length > CvConstants.MaxLength
                ? cv.ExtractedData[..CvConstants.MaxLength]
                : cv.ExtractedData;

            // Gather personalized context from CV feedback, skills gap, and job
            // recommendations. Best-effort: each source is fetched in parallel
            // and silently skipped on failure so the interview still works with
            // just the CV excerpt if none are available.
            var focus = await GatherFocusContextAsync(userId);

            var summaryContextJson = JsonSerializer.Serialize(new
            {
                cvExcerpt,
                targetRole = request.TargetRole,
                cvScore = focus.CvScore,
                cvWeaknesses = focus.CvWeaknesses,
                missingKeywords = focus.MissingKeywords,
                highPriorityGaps = focus.HighPriorityGaps,
                seniorityGap = focus.SeniorityGap,
                jobMissingSkills = focus.JobMissingSkills
            }, ContextJsonOptions);

            // Generate the first question BEFORE any DB write so a fatal LLM
            // error (401, config) leaves no orphan session row. The LLM call
            // sits outside the transaction to avoid long-held locks (Phase 4, H3).
            var questionResult = await _llmService.GenerateNextQuestionAsync(
                track, difficulty, request.TargetRole, summaryContextJson,
                new List<InterviewMessage>(), nextTurnNumber: 1);

            // Both writes (session + first message) are atomic in one TX so a
            // failure between them rolls the session back (Phase 4, H3).
            var strategy = _context.Database.CreateExecutionStrategy();
            var sessionId = await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                var session = new InterviewSession
                {
                    UserId = userId,
                    Track = track,
                    Difficulty = difficulty,
                    TargetRole = request.TargetRole,
                    Status = InterviewStatus.Active,
                    SummaryContextJson = summaryContextJson,
                    UsedFallback = questionResult.UsedFallback,
                    FallbackCount = questionResult.UsedFallback ? 1 : 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InterviewSessions.Add(session);
                await _context.SaveChangesAsync();

                var message = new InterviewMessage
                {
                    SessionId = session.Id,
                    Role = MessageRole.Interviewer,
                    TurnNumber = 1,
                    Content = questionResult.Question,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InterviewMessages.Add(message);
                session.QuestionsAsked = 1;
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return session.Id;
            });

            _logger.LogInformation("Started interview session {SessionId} for user {UserId} — track {Track}, difficulty {Difficulty}", sessionId, userId, track, difficulty);

            return await LoadSessionDtoAsync(sessionId);
        }

        public async Task<InterviewSessionDto?> GetActiveSessionAsync(string userId)
        {
            var session = await _context.InterviewSessions
                .Where(s => s.UserId == userId && s.Status == InterviewStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session is null) return null;

            return await LoadSessionDtoAsync(session.Id);
        }

        public async Task<HintResponseDto> GetHintAsync(string userId, int sessionId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status != InterviewStatus.Active)
            {
                _logger.LogWarning("GetHint blocked: session {SessionId} status is {Status}, not Active.", session.Id, session.Status);
                throw new InvalidOperationException("Session is not active.");
            }

            var currentQuestion = session.Messages
                .Where(m => m.Role == MessageRole.Interviewer)
                .OrderByDescending(m => m.TurnNumber)
                .FirstOrDefault()
                ?.Content;

            if (string.IsNullOrWhiteSpace(currentQuestion))
            {
                _logger.LogWarning("GetHint blocked: session {SessionId} has no interviewer question.", session.Id);
                throw new InvalidOperationException("No active question to provide a hint for.");
            }

            _logger.LogInformation("Generating hint for session {SessionId}, question turn {Turn}.", session.Id, session.QuestionsAsked);

            return await _llmService.GenerateHintAsync(
                session.Track,
                session.Difficulty,
                session.TargetRole,
                currentQuestion,
                session.SummaryContextJson ?? "{}");
        }

        public async Task<InterviewSessionDto> SubmitAnswerAsync(string userId, int sessionId, SubmitAnswerRequestDto request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();

                    var session = await _context.InterviewSessions
                        .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                        ?? throw new KeyNotFoundException("Session not found.");

                    if (session.Status != InterviewStatus.Active)
                    {
                        _logger.LogWarning("SubmitAnswer blocked: session {SessionId} status is {Status}, not Active.", session.Id, session.Status);
                        throw new InvalidOperationException("Session is not active.");
                    }

                    // Concurrency guard: re-check that no concurrent request
                    // already advanced the turn (RowVersion would also catch this,
                    // but the in-memory guard gives a cleaner 409 vs DbConcurrency).
                    var currentDbState = await _context.InterviewSessions
                        .Where(s => s.Id == sessionId)
                        .Select(s => new { s.QuestionsAsked, s.Status })
                        .FirstAsync();

                    if (currentDbState.Status != InterviewStatus.Active
                        || currentDbState.QuestionsAsked != session.QuestionsAsked)
                    {
                        throw new ConflictException("This session was already advanced by another request. Please reload and try again.");
                    }

                    var turnNumber = session.QuestionsAsked;

                    // 1) Persist the candidate's answer BEFORE any AI call so a later
                    //    LLM/transient failure can never lose user input (Phase 2, C2).
                    //    QuestionsAsked is intentionally NOT incremented here — only an
                    //    Interviewer question advances the turn (see StartSessionAsync).
                    var answer = new InterviewMessage
                    {
                        SessionId = session.Id,
                        Role = MessageRole.Candidate,
                        TurnNumber = turnNumber,
                        Content = request.Answer,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InterviewMessages.Add(answer);
                    await _context.SaveChangesAsync();

                    // 2) Final turn: complete the session. No LLM call needed.
                    if (turnNumber >= session.MaxQuestions)
                    {
                        session.Status = InterviewStatus.Completed;
                        session.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        _logger.LogInformation("Session {SessionId} completed after {Questions} questions.", session.Id, turnNumber);
                        return await LoadSessionDtoAsync(session.Id);
                    }

                    // 3) Intermediate turn: reload the full transcript from DB (now
                    //    includes the just-saved answer) and ask the LLM for the next
                    //    question. If this call fails, the answer is already durable;
                    //    the client may retry the submit and the LLM will be retried.
                    var transcript = await _context.InterviewMessages
                        .Where(m => m.SessionId == session.Id)
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();

                    var nextTurn = turnNumber + 1;

                    var questionResult = await _llmService.GenerateNextQuestionAsync(
                        session.Track, session.Difficulty, session.TargetRole,
                        session.SummaryContextJson ?? "{}", transcript, nextTurn);

                    if (questionResult.UsedFallback)
                    {
                        session.UsedFallback = true;
                        session.FallbackCount++;
                    }

                    var questionMsg = new InterviewMessage
                    {
                        SessionId = session.Id,
                        Role = MessageRole.Interviewer,
                        TurnNumber = nextTurn,
                        Content = questionResult.Question,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InterviewMessages.Add(questionMsg);
                    session.QuestionsAsked = nextTurn;
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();

                    _logger.LogInformation("Session {SessionId} advanced to turn {Turn}.", session.Id, nextTurn);

                    return await LoadSessionDtoAsync(session.Id);
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                // RowVersion token fired: a concurrent request advanced the turn
                // in the narrow window between our in-TX re-check and commit.
                // Surface as 409 so the client reloads the now-advanced session
                // (Phase 3 Gap A close, folded into Phase 4).
                throw new ConflictException("This session was already advanced by another request. Please reload and try again.");
            }
        }

        public async IAsyncEnumerable<StreamTokenDto> SubmitAnswerStreamAsync(
            string userId,
            int sessionId,
            SubmitAnswerRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            InterviewMessage? nextQuestion = null;
            string? cvExcerpt = null;
            InterviewTrack track = default;
            InterviewDifficulty difficulty = default;
            string targetRole = string.Empty;
            List<InterviewMessage> fullTranscript = new();
            bool isFinalTurn = false;
            int nextTurn = 0;
            int sessionIdLocal = 0;
            bool usedFallback = false;
            var fullContent = new StringBuilder();
            var tokensEmitted = false;
            Exception? lastStreamError = null;

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

                    var session = await _context.InterviewSessions
                        .Include(s => s.Messages)
                        .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken)
                        ?? throw new KeyNotFoundException("Session not found.");

                    if (session.Status != InterviewStatus.Active)
                    {
                        _logger.LogWarning("SubmitAnswerStream blocked: session {SessionId} status is {Status}, not Active.", sessionId, session.Status);
                        throw new InvalidOperationException("Session is not active.");
                    }

                    var currentDbState = await _context.InterviewSessions
                        .Where(s => s.Id == sessionId)
                        .Select(s => new { s.QuestionsAsked, s.Status })
                        .FirstAsync(cancellationToken);

                    if (currentDbState.Status != InterviewStatus.Active
                        || currentDbState.QuestionsAsked != session.QuestionsAsked)
                    {
                        throw new ConflictException("This session was already advanced by another request. Please reload and try again.");
                    }

                    var turnNumber = session.QuestionsAsked;

                    var answer = new InterviewMessage
                    {
                        SessionId = session.Id,
                        Role = MessageRole.Candidate,
                        TurnNumber = turnNumber,
                        Content = request.Answer,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InterviewMessages.Add(answer);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (turnNumber >= session.MaxQuestions)
                    {
                        session.Status = InterviewStatus.Completed;
                        session.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                        await tx.CommitAsync(cancellationToken);
                        _logger.LogInformation("Session {SessionId} completed after {Questions} questions (stream).", session.Id, turnNumber);
                        isFinalTurn = true;
                    }
                    else
                    {
                        fullTranscript = await _context.InterviewMessages
                            .Where(m => m.SessionId == session.Id)
                            .OrderBy(m => m.CreatedAt)
                            .ToListAsync(cancellationToken);

                        cvExcerpt = session.SummaryContextJson ?? "{}";
                        track = session.Track;
                        difficulty = session.Difficulty;
                        targetRole = session.TargetRole;
                        nextTurn = turnNumber + 1;
                        sessionIdLocal = session.Id;

                        await tx.CommitAsync(cancellationToken);
                    }
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("This session was already advanced by another request. Please reload and try again.");
            }

            if (isFinalTurn)
            {
                yield return new StreamTokenDto { Type = "done" };
                yield break;
            }

            _logger.LogInformation("Streaming question for session {SessionId}, turn {Turn}.", sessionIdLocal, nextTurn);

            await foreach (var token in _llmService.GenerateNextQuestionStreamAsync(
                track, difficulty, targetRole, cvExcerpt ?? "{}", fullTranscript, nextTurn, cancellationToken))
            {
                if (token.Type == "token" && token.Content is not null)
                {
                    tokensEmitted = true;
                    fullContent.Append(token.Content);
                }
                if (token.Type == "error" && token.Code == "fallback")
                {
                    usedFallback = true;
                }
                lastStreamError = token.Type == "error" ? new Exception(token.Message ?? "Stream error") : lastStreamError;
                yield return token;
            }

            if (!tokensEmitted)
            {
                _logger.LogError(lastStreamError, "Stream completed without any tokens for session {SessionId}, turn {Turn}.", sessionIdLocal, nextTurn);
                yield return new StreamTokenDto { Type = "error", Code = "fatal", Message = "AI returned no content." };
                yield return new StreamTokenDto { Type = "done" };
                yield break;
            }

            var streamConflict = false;
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

                    var session = await _context.InterviewSessions
                        .FirstOrDefaultAsync(s => s.Id == sessionIdLocal, cancellationToken)
                        ?? throw new KeyNotFoundException("Session disappeared mid-stream.");

                    var question = new InterviewMessage
                    {
                        SessionId = sessionIdLocal,
                        Role = MessageRole.Interviewer,
                        TurnNumber = nextTurn,
                        Content = fullContent.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InterviewMessages.Add(question);
                    session.QuestionsAsked = nextTurn;
                    if (usedFallback)
                    {
                        session.UsedFallback = true;
                        session.FallbackCount++;
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                });
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                _logger.LogWarning(concurrencyEx, "Stream question save hit RowVersion conflict on session {SessionId}, turn {Turn}.", sessionIdLocal, nextTurn);
                streamConflict = true;
            }

            if (streamConflict)
            {
                yield return new StreamTokenDto { Type = "error", Code = "fallback", Message = "Session was advanced by another request. Reload to continue." };
                yield return new StreamTokenDto { Type = "done" };
                yield break;
            }

            _logger.LogInformation(
                "Streamed question for session {SessionId}, turn {Turn}, usedFallback={UsedFallback}.",
                sessionIdLocal, nextTurn, usedFallback);

            yield return new StreamTokenDto { Type = "done" };
        }

        public async Task DeleteSessionAsync(string userId, int sessionId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                var session = await _context.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                    ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

                _context.InterviewSessions.Remove(session);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
            });

            _logger.LogInformation("Deleted interview session {SessionId} for user {UserId}.", sessionId, userId);
        }

        public async Task<InterviewScorecardDto> GetScorecardAsync(string userId, int sessionId)
        {
            var session = await _context.InterviewSessions
                .AsNoTracking()
                .Include(s => s.Scorecard)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status != InterviewStatus.Completed)
            {
                _logger.LogWarning("GetScorecard blocked: session {SessionId} status is {Status}, not Completed.", sessionId, session.Status);
                throw new InvalidOperationException("Session is not yet completed. Answer all questions first.");
            }

            if (session.Scorecard is not null)
            {
                _logger.LogInformation("Returning cached scorecard for session {SessionId}.", sessionId);
                return MapScorecardToDto(session.Scorecard);
            }

            _logger.LogInformation("Generating scorecard for session {SessionId}...", sessionId);

            var transcript = await _context.InterviewMessages
                .AsNoTracking()
                .Where(m => m.SessionId == session.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Pass the full SummaryContextJson snapshot so the evaluator can
            // see both the CV excerpt AND the personalized focus areas (M5 +
            // context-rich interview). The prompt builder extracts what it needs.
            // LLM call outside any transaction to avoid long-held locks (Phase 4, H3).
            var dto = await _llmService.GenerateScorecardAsync(
                transcript, session.Track, session.Difficulty, session.TargetRole, session.SummaryContextJson ?? "{}");

            // Atomic write: re-check inside the TX so a concurrent request that
            // already inserted short-circuits; the unique index on SessionId is the
            // hard guard, caught below for the narrow post-re-check race (Phase 4, H3).
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                var existing = await _context.InterviewScorecards
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sc => sc.SessionId == sessionId);
                if (existing is not null)
                {
                    _logger.LogInformation("Scorecard for session {SessionId} was generated concurrently; returning existing.", sessionId);
                    await tx.CommitAsync();
                    return MapScorecardToDto(existing);
                }

                var scorecard = new InterviewScorecard
                {
                    SessionId = sessionId,
                    OverallScore = dto.OverallScore,
                    LetterGrade = dto.LetterGrade,
                    OverallSummary = dto.OverallSummary,
                    StrengthsJson = JsonSerializer.Serialize(dto.Strengths),
                    ImprovementsJson = JsonSerializer.Serialize(dto.AreasForImprovement),
                    QuestionAnalysisJson = JsonSerializer.Serialize(dto.QuestionAnalysis),
                    GeneratedAt = DateTime.UtcNow
                };

                try
                {
                    _context.InterviewScorecards.Add(scorecard);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    // Lost the race to the unique index on SessionId — re-read and
                    // return the winner's scorecard. Clear the change tracker so the
                    // discarded Add doesn't linger (all other reads are AsNoTracking).
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning("Concurrent scorecard insert for session {SessionId} lost the race; returning existing.", sessionId);
                    var winner = await _context.InterviewScorecards
                        .AsNoTracking()
                        .FirstAsync(sc => sc.SessionId == sessionId);
                    await tx.CommitAsync();
                    return MapScorecardToDto(winner);
                }

                _logger.LogInformation("Scorecard generated for session {SessionId} — grade {Grade}.", sessionId, dto.LetterGrade);
                return dto;
            });
        }

        public async Task<List<InterviewHistoryItemDto>> GetHistoryAsync(string userId)
        {
            var sessions = await _context.InterviewSessions
                .Where(s => s.UserId == userId && s.Status != InterviewStatus.Active)
                .Include(s => s.Scorecard)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new InterviewHistoryItemDto
                {
                    Id = s.Id,
                    Track = EnumDisplay.Name(s.Track),
                    Difficulty = EnumDisplay.Name(s.Difficulty),
                    TargetRole = s.TargetRole,
                    Status = s.Status.ToString(),
                    QuestionsAsked = s.QuestionsAsked,
                    MaxQuestions = s.MaxQuestions,
                    OverallScore = s.Scorecard != null ? s.Scorecard.OverallScore : null,
                    LetterGrade = s.Scorecard != null ? s.Scorecard.LetterGrade : null,
                    OverallSummary = s.Scorecard != null ? s.Scorecard.OverallSummary : null,
                    CreatedAt = s.CreatedAt,
                    CompletedAt = s.CompletedAt
                })
                .ToListAsync();

            return sessions;
        }

        private async Task<InterviewSessionDto> LoadSessionDtoAsync(int sessionId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id))
                .FirstOrDefaultAsync(s => s.Id == sessionId)
                ?? throw new KeyNotFoundException("Session not found.");

            return new InterviewSessionDto
            {
                Id = session.Id,
                Track = EnumDisplay.Name(session.Track),
                Difficulty = EnumDisplay.Name(session.Difficulty),
                TargetRole = session.TargetRole,
                Status = session.Status.ToString(),
                QuestionsAsked = session.QuestionsAsked,
                MaxQuestions = session.MaxQuestions,
                Messages = session.Messages.Select(m => new InterviewMessageDto
                {
                    Id = m.Id,
                    Role = m.Role.ToString(),
                    TurnNumber = m.TurnNumber,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                CreatedAt = session.CreatedAt,
                CompletedAt = session.CompletedAt
            };
        }

        public async Task<UserRoadmapDto> ConvertScorecardToRoadmapAsync(string userId, int sessionId)
        {
            var scorecard = await GetScorecardAsync(userId, sessionId);

            if (scorecard.AreasForImprovement == null || scorecard.AreasForImprovement.Count == 0)
                throw new InvalidOperationException(
                    "No areas for improvement to convert. The candidate performed strongly on this interview.");

            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync();

            var cvText = cv?.ExtractedData ?? string.Empty;

            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            var targetRole = session?.TargetRole ?? "Software Engineer";

            var newSteps = await _roadmapLlmService.GenerateWeaknessStepsAsync(
                scorecard.AreasForImprovement, cvText, targetRole);

            var updatedRoadmap = await _userRoadmapService.AppendWeaknessStepsAsync(userId, newSteps);

            _logger.LogInformation(
                "Converted scorecard for session {SessionId} to {Count} roadmap steps for user {UserId}.",
                sessionId, newSteps.Count, userId);

            return updatedRoadmap;
        }

        private static InterviewScorecardDto MapScorecardToDto(InterviewScorecard sc)
        {
            return new InterviewScorecardDto
            {
                OverallScore = sc.OverallScore,
                LetterGrade = sc.LetterGrade,
                OverallSummary = sc.OverallSummary,
                Strengths = JsonSerializer.Deserialize<List<string>>(sc.StrengthsJson) ?? new(),
                AreasForImprovement = JsonSerializer.Deserialize<List<string>>(sc.ImprovementsJson) ?? new(),
                QuestionAnalysis = JsonSerializer.Deserialize<List<QuestionAnalysisItemDto>>(sc.QuestionAnalysisJson) ?? new()
            };
        }

        private static T ParseEnum<T>(string value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
                return result;

            throw new ArgumentException($"Invalid value '{value}' for {typeof(T).Name}. Valid values: {string.Join(", ", Enum.GetNames<T>())}");
        }

        /// <summary>
        /// True when <paramref name="ex"/> wraps a SQL Server unique-constraint
        /// (2627) or unique-index (2601) violation. Used to turn a lost scorecard
        /// insert race into a graceful "return existing" path (Phase 4, H3).
        /// </summary>
        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sql && sql.Number is 2627 or 2601;
        }

        /// <summary>
        /// Gathers personalized context from CV feedback, skills-gap roadmap,
        /// and job recommendations in parallel. Each source is best-effort: a
        /// failure (cache miss, service error) is logged and silently skipped
        /// so the interview still works with whatever context is available.
        /// Used by <see cref="StartSessionAsync"/> (full JSON snapshot) and
        /// <see cref="GetOptionsAsync"/> (FocusAreas list only).
        /// </summary>
        private async Task<InterviewFocusContext> GatherFocusContextAsync(string userId)
        {
            CvFeedbackDto? feedback = null;
            UserRoadmapDto? roadmap = null;
            JobRecommendationResultDto? jobs = null;

            var feedbackTask = TryCatchAsync(
                () => _cvFeedbackService.GetFeedbackAsync(userId),
                "CV feedback", userId);
            var roadmapTask = TryCatchAsync(
                () => _userRoadmapService.GetMyRoadmapAsync(userId),
                "roadmap", userId);
            var jobsTask = TryCatchAsync(
                () => _jobRecommendationService.GetRecommendationsAsync(userId),
                "job recommendations", userId);

            await Task.WhenAll(feedbackTask, roadmapTask, jobsTask);
            feedback = feedbackTask.Result;
            roadmap = roadmapTask.Result;
            jobs = jobsTask.Result;

            // CV feedback → score + high-priority weaknesses + missing keywords
            int? cvScore = feedback?.OverallScore > 0 ? feedback!.OverallScore : null;

            var cvWeaknesses = feedback?.Suggestions
                .OrderByDescending(s => string.Equals(s.Priority, "High", StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(s => Truncate(s.Issue, 100))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList() ?? new List<string>();

            var missingKeywords = feedback?.MissingKeywords.Take(5).ToList() ?? new List<string>();

            // Roadmap → high-priority skill gaps + seniority gap
            var allGaps = roadmap?.GapAnalysis
                .SelectMany(c => c.Skills)
                .ToList() ?? new List<SkillGapItemDto>();

            var highPriorityGaps = allGaps
                .OrderByDescending(g => string.Equals(g.Priority, "High", StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(g => new GapSummary(
                    g.SkillName,
                    g.CurrentLevel,
                    g.RequiredLevel,
                    Truncate(g.Gap, 80)))
                .ToList();

            string? seniorityGap = string.IsNullOrWhiteSpace(roadmap?.SeniorityGap)
                ? null
                : roadmap!.SeniorityGap;

            // Job recommendations → merge missing skills from top 3 jobs
            var jobMissingSkills = jobs?.Recommendations
                .Take(3)
                .SelectMany(j => j.MissingSkills)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList() ?? new List<string>();

            // FocusAreas = combined unique skill names from gaps + job missing skills
            var focusAreas = highPriorityGaps
                .Select(g => g.Skill)
                .Concat(jobMissingSkills)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            if (focusAreas.Count == 0 && cvWeaknesses.Count > 0)
            {
                // No skill gaps but CV has weaknesses — surface a general label
                focusAreas.Add("CV-identified weaknesses");
            }

            _logger.LogInformation(
                "Gathered interview context for user {UserId}: cvScore={CvScore}, cvWeaknesses={CvWeaknesses}, gaps={Gaps}, jobSkills={JobSkills}, focusAreas={FocusAreas}",
                userId, cvScore, cvWeaknesses.Count, highPriorityGaps.Count, jobMissingSkills.Count, focusAreas.Count);

            return new InterviewFocusContext(
                focusAreas,
                cvScore,
                cvWeaknesses,
                missingKeywords,
                highPriorityGaps,
                seniorityGap,
                jobMissingSkills);
        }

        /// <summary>
        /// Runs a task and swallows exceptions, logging a warning. Used by
        /// <see cref="GatherFocusContextAsync"/> for best-effort context fetches.
        /// </summary>
        private async Task<T?> TryCatchAsync<T>(Func<Task<T?>> factory, string label, string userId) where T : class
        {
            try
            {
                return await factory();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to gather {Label} for interview context (user {UserId}): {Error}", label, userId, ex.Message);
                return null;
            }
        }

        private static string Truncate(string? text, int max) =>
            string.IsNullOrWhiteSpace(text) ? string.Empty :
            text.Length <= max ? text : text[..max] + "…";

        private static readonly JsonSerializerOptions ContextJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Structured personalized context gathered from CV feedback, roadmap,
        /// and job recommendations. Serialized into <see cref="InterviewSession.SummaryContextJson"/>
        /// at session start and fed to the LLM prompt builders.
        /// </summary>
        private record InterviewFocusContext(
            List<string> FocusAreas,
            int? CvScore,
            List<string> CvWeaknesses,
            List<string> MissingKeywords,
            List<GapSummary> HighPriorityGaps,
            string? SeniorityGap,
            List<string> JobMissingSkills);

        /// <summary>
        /// A single skill-gap item surfaced to the LLM as a focus area.
        /// </summary>
        private record GapSummary(
            string Skill,
            string CurrentLevel,
            string RequiredLevel,
            string Gap);

        /// <summary>
        /// Extracts the <c>cvExcerpt</c> field from the session's
        /// <see cref="InterviewSession.SummaryContextJson"/> snapshot (stored at
        /// session start; contains cvExcerpt + targetRole + personalized focus
        /// areas) and truncates it to 1000 chars to bound the scorecard prompt
        /// token budget (Phase 7, M5). Returns empty string on any parse
        /// failure so the prompt degrades gracefully (no CV context section).
        /// </summary>
        private static string ExtractCvExcerpt(string? summaryContextJson)
        {
            if (string.IsNullOrWhiteSpace(summaryContextJson))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(summaryContextJson);
                var excerpt = doc.RootElement.TryGetProperty("cvExcerpt", out var el) && el.ValueKind == JsonValueKind.String
                    ? el.GetString() ?? string.Empty
                    : string.Empty;
                return excerpt.Length > CvConstants.MaxLength ? excerpt[..CvConstants.MaxLength] : excerpt;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
    }
}
