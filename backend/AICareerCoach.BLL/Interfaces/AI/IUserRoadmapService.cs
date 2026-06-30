using AICareerCoach.BLL.DTOs.Roadmap;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IUserRoadmapService
    {
        Task<UserRoadmapDto> GenerateRoadmapAsync(string userId, GenerateRoadmapRequestDto request);
        Task<UserRoadmapDto?> GetMyRoadmapAsync(string userId);
        Task<UserRoadmapDto> RescanGapAnalysisAsync(string userId);
        Task<UserRoadmapDto> AppendWeaknessStepsAsync(string userId, List<RoadmapStepResultDto> newSteps);
    }
}
