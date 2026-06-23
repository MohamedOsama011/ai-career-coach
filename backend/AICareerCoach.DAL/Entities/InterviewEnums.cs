namespace AICareerCoach.DAL.Entities
{
    /// <summary>Interview track selected by the candidate at setup.</summary>
    public enum InterviewTrack
    {
        Behavioral,
        TechnicalCoding,
        SystemDesign
    }

    /// <summary>Seniority calibration for question difficulty.</summary>
    public enum InterviewDifficulty
    {
        Junior,
        MidLevel,
        Senior
    }

    /// <summary>Lifecycle state of an interview session.</summary>
    public enum InterviewStatus
    {
        Active,
        Completed,
        Abandoned
    }

    /// <summary>Author of a message in the interview transcript.</summary>
    public enum MessageRole
    {
        Interviewer,
        Candidate
    }
}
