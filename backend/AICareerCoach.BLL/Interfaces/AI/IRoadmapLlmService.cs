using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IRoadmapLlmService
    {
        Task<(List<RoadmapStepResultDto> Steps, List<SkillsCategoryDto> GapAnalysis)> GenerateRoadmapAsync(
            string cvText, string targetRole, Roadmap template);
    }
}
