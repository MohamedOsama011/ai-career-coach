using AICareerCoach.BLL.DTOs.Admin;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IAdminRoadmapService
    {
        Task<List<RoadmapTemplateDto>> GetAllTemplatesAsync();
        Task<RoadmapTemplateDto> GetTemplateAsync(int id);
        Task<RoadmapTemplateDto> CreateTemplateAsync(CreateRoadmapTemplateDto dto);
        Task<RoadmapTemplateDto> UpdateTemplateAsync(int id, UpdateRoadmapTemplateDto dto);
        Task DeleteTemplateAsync(int id);
        Task<TestMatchResultDto> TestMatchAsync(int id, string? sampleCvText);
    }
}
