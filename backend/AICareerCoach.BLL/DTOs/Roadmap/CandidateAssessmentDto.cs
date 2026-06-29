namespace AICareerCoach.BLL.DTOs.Roadmap
{
    /// <summary>
    /// Seniority assessment produced by Pass 1 of the two-pass roadmap
    /// generation pipeline. Persisted as <see cref="UserRoadmap.AssessmentJson"/>
    /// and surfaced on <see cref="UserRoadmapDto"/> so the UI can show a
    /// "Current → Target" seniority chip.
    /// </summary>
    public class CandidateAssessmentDto
    {
        /// <summary>Seniority level inferred from CV evidence (Junior / Mid / Senior).</summary>
        public string CurrentSeniority { get; set; } = string.Empty;

        /// <summary>Seniority level implied by the target role string (Junior / Mid / Senior).</summary>
        public string TargetSeniority { get; set; } = string.Empty;

        /// <summary>Human-readable description of the seniority jump
        /// (e.g. "Mid to Senior — focus on architecture & system design").</summary>
        public string SeniorityGap { get; set; } = string.Empty;
    }
}
