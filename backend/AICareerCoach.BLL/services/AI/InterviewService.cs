using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.Exceptions;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AICareerCoach.BLL.Services.AI
{
    public class InterviewService : IInterviewService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IInterviewLlmService _llmService;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(
            AICareerCoachDbContext context,
            IInterviewLlmService llmService,
            ILogger<InterviewService> logger)
        {
            _context = context;
            _llmService = llmService;
            _logger = logger;
        }

        public Task<InterviewOptionsDto> GetOptionsAsync()
        {
            var tracks = new List<InterviewOptionItem>
            {
                new() { Value = "Behavioral", Label = "Behavioral" },
                new() { Value = "TechnicalCoding", Label = "Technical Coding" },
                new() { Value = "SystemDesign", Label = "System Design" }
            };

            var difficulties = new List<InterviewOptionItem>
            {
                new() { Value = "Junior", Label = "Junior" },
                new() { Value = "MidLevel", Label = "Mid-Level" },
                new() { Value = "Senior", Label = "Senior" }
            };

            return Task.FromResult(new InterviewOptionsDto
            {
                Tracks = tracks,
                Difficulties = difficulties
            });
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

            var cvExcerpt = cv.ExtractedData.Length > 4000
                ? cv.ExtractedData[..4000]
                : cv.ExtractedData;

            var summaryContextJson = JsonSerializer.Serialize(new
            {
                cvExcerpt,
                targetRole = request.TargetRole
            });

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

            // M5: pass a bounded (~1000-char) CV excerpt from the session's
            // SummaryContextJson snapshot so the evaluator can assess whether
            // the candidate accurately represented their experience.
            var cvExcerpt = ExtractCvExcerpt(session.SummaryContextJson);

            // LLM call outside any transaction to avoid long-held locks (Phase 4, H3).
            var dto = await _llmService.GenerateScorecardAsync(
                transcript, session.Track, session.Difficulty, session.TargetRole, cvExcerpt);

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
                    Track = s.Track.ToString(),
                    Difficulty = s.Difficulty.ToString(),
                    TargetRole = s.TargetRole,
                    Status = s.Status.ToString(),
                    QuestionsAsked = s.QuestionsAsked,
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
                Track = session.Track.ToString(),
                Difficulty = session.Difficulty.ToString(),
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
        /// Extracts the <c>cvExcerpt</c> field from the session's
        /// <see cref="InterviewSession.SummaryContextJson"/> snapshot (stored at
        /// session start as <c>{ cvExcerpt, targetRole }</c>) and truncates it to
        /// 1000 chars to bound the scorecard prompt token budget (Phase 7, M5).
        /// Returns empty string on any parse failure so the prompt degrades
        /// gracefully (no CV context section).
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
                return excerpt.Length > 1000 ? excerpt[..1000] : excerpt;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
    }
}
