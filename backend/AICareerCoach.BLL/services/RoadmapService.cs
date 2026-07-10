using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICareerCoach.BLL.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IRoadmapRepository _roadmapRepo;
        private readonly AICareerCoachDbContext _context;
        private readonly IEmbeddingService _embeddingService;

        public RoadmapService(IRoadmapRepository roadmapRepo, AICareerCoachDbContext context, IEmbeddingService embeddingService)
        {
            _roadmapRepo = roadmapRepo;
            _context = context;
            _embeddingService = embeddingService;
        }

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

        public async Task IndexTemplateEmbeddingsAsync()
        {
            var templates = await _context.Roadmaps.Include(r => r.Steps).ToListAsync();

            foreach (var template in templates)
            {
                string stepsText = string.Join("\n    ", template.Steps.OrderBy(s => s.OrderIndex).Select(s =>
                    $"- {s.Title}: {s.Description} [Level: {s.Level}]"
                ));
                string combinedText = $"Track: {template.Track}\nTitle: {template.Title}\nDescription: {template.Description}\n\nSteps:\n    {stepsText}";
                var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(combinedText);

                var existingEmbedding = await _context.RoadmapTemplateEmbeddings
                    .FirstOrDefaultAsync(e => e.RoadmapId == template.Id);

                if (existingEmbedding != null)
                {
                    existingEmbedding.Embedding = embeddingVector;
                    existingEmbedding.ComputedAt = DateTime.UtcNow;
                }
                else
                {
                    var newEmbedding = new RoadmapTemplateEmbedding
                    {
                        RoadmapId = template.Id,
                        Embedding = embeddingVector,
                        ComputedAt = DateTime.UtcNow
                    };
                    await _context.RoadmapTemplateEmbeddings.AddAsync(newEmbedding);
                }

                await Task.Delay(4500);
            }

            await _context.SaveChangesAsync();
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
