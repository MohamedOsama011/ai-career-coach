using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using System.Text.Json;

namespace AICareerCoach.BLL.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IRoadmapRepository _roadmapRepo;

        public RoadmapService(IRoadmapRepository roadmapRepo) => _roadmapRepo = roadmapRepo;

        public async Task<List<RoadmapDto>> GetAllAsync(string? track)
        {
            var roadmaps = await _roadmapRepo.GetAllWithStepsAsync(track);
            return roadmaps.Select(MapToDto).ToList();
        }

        public async Task<RoadmapDto> GetByIdAsync(int id)
        {
            var roadmap = await _roadmapRepo.GetByIdWithStepsAsync(id);
            if (roadmap is null) throw new KeyNotFoundException($"Roadmap with id {id} not found.");
            return MapToDto(roadmap);
        }

        public async Task<RoadmapDto> CreateAsync(CreateRoadmapDto dto)
        {
            var roadmap = new Roadmap
            {
                Track = dto.Track,
                Title = dto.Title,
                Description = dto.Description,
                OrderIndex = dto.OrderIndex,
                Steps = dto.Steps.Select(s => new RoadmapStep
                {
                    Title = s.Title,
                    Description = s.Description,
                    Level = s.Level,
                    Resources = JsonSerializer.Serialize(s.Resources),
                    OrderIndex = s.OrderIndex
                }).ToList()
            };

            await _roadmapRepo.AddAsync(roadmap);
            return MapToDto(roadmap);
        }

        private static RoadmapDto MapToDto(Roadmap r) => new()
        {
            Id = r.Id,
            Track = r.Track,
            Title = r.Title,
            Description = r.Description,
            OrderIndex = r.OrderIndex,
            Steps = r.Steps.Select(s => new RoadmapStepDto
            {
                Id = s.Id,
                RoadmapId = s.RoadmapId,
                Title = s.Title,
                Description = s.Description,
                Level = s.Level,
                Resources = JsonSerializer.Deserialize<List<string>>(s.Resources) ?? new(),
                OrderIndex = s.OrderIndex
            }).ToList()
        };
    }
}
