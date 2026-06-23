using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    /// <summary>
    /// One stateful mock-interview run for a user. Holds the locked
    /// configuration (track, difficulty, target role) and the stable AI
    /// context snapshot taken from the candidate's CV at start time.
    /// </summary>
    public class InterviewSession
    {
        public int Id { get; set; }

        /// <summary>FK → AspNetUsers (nvarchar(450)).</summary>
        public string UserId { get; set; } = string.Empty;

        public string TargetRole { get; set; } = string.Empty;

        public InterviewTrack Track { get; set; }

        public InterviewDifficulty Difficulty { get; set; }

        public InterviewStatus Status { get; set; } = InterviewStatus.Active;

        public int MaxQuestions { get; set; } = 6;

        public int QuestionsAsked { get; set; }

        /// <summary>
        /// Snapshot of { cvHash, cvExcerpt, targetRole } used as stable,
        /// compact context for every LLM turn (no re-extracting the CV).
        /// </summary>
        public string? SummaryContextJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<InterviewMessage> Messages { get; set; } = new();

        public InterviewScorecard? Scorecard { get; set; }

        public User? User { get; set; }
    }

    /// <summary>
    /// One message in the interview transcript — either an Interviewer
    /// question or a Candidate answer. Persisted immediately so a session
    /// can be resumed after a reload or transient AI failure.
    /// </summary>
    public class InterviewMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        public MessageRole Role { get; set; }

        /// <summary>1..MaxQuestions for questions; answers share their question's turn.</summary>
        public int TurnNumber { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public InterviewSession Session { get; set; } = null!;
    }

    /// <summary>
    /// Final structured evaluation for a completed session (1:0..1 with
    /// InterviewSession). Generated lazily on first scorecard request and
    /// cached here. Structured output serialized into *Json columns like
    /// <see cref="UserRoadmap.StepsJson"/>.
    /// </summary>
    public class InterviewScorecard
    {
        public int Id { get; set; }

        /// <summary>Unique FK → InterviewSessions (effectively 1:1).</summary>
        public int SessionId { get; set; }

        public int OverallScore { get; set; }

        /// <summary>A / A- / B+ / B / C.</summary>
        public string LetterGrade { get; set; } = string.Empty;

        public string OverallSummary { get; set; } = string.Empty;

        /// <summary>Serialized List&lt;string&gt;.</summary>
        public string StrengthsJson { get; set; } = "[]";

        /// <summary>Serialized List&lt;string&gt;.</summary>
        public string ImprovementsJson { get; set; } = "[]";

        /// <summary>Serialized List&lt;QuestionAnalysisDto&gt; (kept as JSON here; DTO lives in BLL).</summary>
        public string QuestionAnalysisJson { get; set; } = "[]";

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public InterviewSession Session { get; set; } = null!;
    }
}
