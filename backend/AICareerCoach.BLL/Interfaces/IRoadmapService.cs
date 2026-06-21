using AICareerCoach.BLL.DTOs.Roadmap;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IRoadmapService
    {
        Task<List<RoadmapDto>> GetAllAsync(string? track);
        Task<RoadmapDto> GetByIdAsync(int id);
        Task<RoadmapDto> CreateAsync(CreateRoadmapDto dto);
        Task IndexTemplateEmbeddingsAsync();
    }
}
