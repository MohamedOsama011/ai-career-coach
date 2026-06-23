using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
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
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Please upload your CV first to start a mock interview.");

            if (string.IsNullOrEmpty(cv.ExtractedData))
                throw new InvalidOperationException("CV text not extracted yet. Please request CV feedback first.");

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

            var session = new InterviewSession
            {
                UserId = userId,
                Track = track,
                Difficulty = difficulty,
                TargetRole = request.TargetRole,
                Status = InterviewStatus.Active,
                SummaryContextJson = summaryContextJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.InterviewSessions.Add(session);
            await _context.SaveChangesAsync();

            var question = await _llmService.GenerateNextQuestionAsync(
                track, difficulty, request.TargetRole, summaryContextJson,
                new List<InterviewMessage>(), nextTurnNumber: 1);

            var message = new InterviewMessage
            {
                SessionId = session.Id,
                Role = MessageRole.Interviewer,
                TurnNumber = 1,
                Content = question,
                CreatedAt = DateTime.UtcNow
            };

            _context.InterviewMessages.Add(message);
            session.QuestionsAsked = 1;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started interview session {SessionId} for user {UserId}", session.Id, userId);

            return await LoadSessionDtoAsync(session.Id);
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
            var session = await _context.InterviewSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status != InterviewStatus.Active)
                throw new InvalidOperationException("Session is not active.");

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

            if (turnNumber >= session.MaxQuestions)
            {
                session.Status = InterviewStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Session {SessionId} completed after {Questions} questions.", session.Id, turnNumber);
            }
            else
            {
                var transcript = await _context.InterviewMessages
                    .Where(m => m.SessionId == session.Id)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                transcript.Add(answer);

                var nextTurn = turnNumber + 1;

                var question = await _llmService.GenerateNextQuestionAsync(
                    session.Track, session.Difficulty, session.TargetRole,
                    session.SummaryContextJson ?? "{}", transcript, nextTurn);

                var questionMsg = new InterviewMessage
                {
                    SessionId = session.Id,
                    Role = MessageRole.Interviewer,
                    TurnNumber = nextTurn,
                    Content = question,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InterviewMessages.Add(questionMsg);
                session.QuestionsAsked = nextTurn;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return await LoadSessionDtoAsync(session.Id);
        }

        public async Task<InterviewScorecardDto> GetScorecardAsync(string userId, int sessionId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.Scorecard)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new KeyNotFoundException("Session not found.");

            if (session.Status != InterviewStatus.Completed)
                throw new InvalidOperationException("Session is not yet completed. Answer all questions first.");

            if (session.Scorecard is not null)
            {
                _logger.LogInformation("Returning cached scorecard for session {SessionId}.", sessionId);
                return MapScorecardToDto(session.Scorecard);
            }

            _logger.LogInformation("Generating scorecard for session {SessionId}...", sessionId);

            var transcript = await _context.InterviewMessages
                .Where(m => m.SessionId == session.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var dto = await _llmService.GenerateScorecardAsync(
                transcript, session.Track, session.Difficulty, session.TargetRole);

            var scorecard = new InterviewScorecard
            {
                SessionId = session.Id,
                OverallScore = dto.OverallScore,
                LetterGrade = dto.LetterGrade,
                OverallSummary = dto.OverallSummary,
                StrengthsJson = JsonSerializer.Serialize(dto.Strengths),
                ImprovementsJson = JsonSerializer.Serialize(dto.AreasForImprovement),
                QuestionAnalysisJson = JsonSerializer.Serialize(dto.QuestionAnalysis),
                GeneratedAt = DateTime.UtcNow
            };

            _context.InterviewScorecards.Add(scorecard);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Scorecard generated for session {SessionId} — grade {Grade}.", sessionId, dto.LetterGrade);

            return dto;
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
                    CreatedAt = s.CreatedAt,
                    CompletedAt = s.CompletedAt
                })
                .ToListAsync();

            return sessions;
        }

        private async Task<InterviewSessionDto> LoadSessionDtoAsync(int sessionId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
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
    }
}
