using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IRoadmapLlmService
    {
        /// <summary>
        /// Two-pass gap-driven roadmap generation.
        /// Pass 1: assesses the candidate's current/target seniority + identifies
        ///   the skill gaps required to reach the target role.
        /// Pass 2: builds personalised steps ONLY for the identified gaps, ordered
        ///   by priority (not difficulty), skipping skills the candidate already has.
        /// The reference <paramref name="template"/> is used for tech-stack context
        /// only — it is NOT a rigid step skeleton.
        /// </summary>
        Task<(List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis, CandidateAssessmentDto? Assessment)> GenerateRoadmapAsync(
            string cvText, string targetRole, Roadmap template);

        /// <summary>
        /// Re-assess the skills gap (and seniority) for an existing roadmap
        /// without regenerating the steps. Used by the "Rescan Gaps" endpoint.
        /// </summary>
        Task<(List<SkillsCategoryDto> GapAnalysis, CandidateAssessmentDto? Assessment)> GenerateGapAnalysisAsync(
            string cvText, string targetRole, Roadmap template);

        Task<List<RoadmapStepResultDto>> GenerateWeaknessStepsAsync(
            List<string> weakAreas, string cvText, string targetRole);
    }
}
