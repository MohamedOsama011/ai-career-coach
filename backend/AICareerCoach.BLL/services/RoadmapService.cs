using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICareerCoach.BLL.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly AICareerCoachDbContext _context;

        public RoadmapService(AICareerCoachDbContext context) => _context = context;

        public async Task<List<RoadmapDto>> GetAllAsync(string? track)
        {
            var query = _context.Roadmaps
                .Include(r => r.Steps.OrderBy(s => s.OrderIndex))
                .AsQueryable();

            if (!string.IsNullOrEmpty(track))
                query = query.Where(r => r.Track == track);

            var roadmaps = await query.OrderBy(r => r.OrderIndex).ToListAsync();

            return roadmaps.Select(MapToDto).ToList();
        }

        public async Task<RoadmapDto> GetByIdAsync(int id)
        {
            var roadmap = await _context.Roadmaps
                .Include(r => r.Steps.OrderBy(s => s.OrderIndex))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (roadmap is null)
                throw new KeyNotFoundException($"Roadmap with id {id} not found.");

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

            _context.Roadmaps.Add(roadmap);
            await _context.SaveChangesAsync();

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
