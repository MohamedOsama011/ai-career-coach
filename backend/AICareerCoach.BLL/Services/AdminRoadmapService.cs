using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AICareerCoach.BLL.Services
{
    public class AdminRoadmapService : IAdminRoadmapService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<AdminRoadmapService> _logger;

        public AdminRoadmapService(
            AICareerCoachDbContext context,
            IEmbeddingService embeddingService,
            ILogger<AdminRoadmapService> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<List<RoadmapTemplateDto>> GetAllTemplatesAsync()
        {
            var templates = await _context.Roadmaps
                .Include(r => r.Steps)
                .OrderBy(r => r.OrderIndex)
                .ToListAsync();

            var embeddingLookup = await _context.RoadmapTemplateEmbeddings
                .Select(e => new { e.RoadmapId, e.ComputedAt })
                .ToListAsync();

            var lookup = embeddingLookup.ToDictionary(e => e.RoadmapId, e => (DateTime?)e.ComputedAt);

            return templates.Select(r => MapToDto(r, lookup.ContainsKey(r.Id), lookup.GetValueOrDefault(r.Id))).ToList();
        }

        public async Task<RoadmapTemplateDto> GetTemplateAsync(int id)
        {
            var template = await _context.Roadmaps
                .Include(r => r.Steps.OrderBy(s => s.OrderIndex))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (template is null)
                throw new KeyNotFoundException($"Roadmap template with id {id} not found.");

            var embedding = await _context.RoadmapTemplateEmbeddings
                .FirstOrDefaultAsync(e => e.RoadmapId == id);

            return MapToDto(template, embedding is not null, embedding?.ComputedAt);
        }

        public async Task<RoadmapTemplateDto> CreateTemplateAsync(CreateRoadmapTemplateDto dto)
        {
            var template = new Roadmap
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

            _context.Roadmaps.Add(template);
            await _context.SaveChangesAsync();

            await GenerateEmbeddingAsync(template);

            _logger.LogInformation("Admin created roadmap template {Id} — {Title}", template.Id, template.Title);

            var embedding = await _context.RoadmapTemplateEmbeddings
                .FirstOrDefaultAsync(e => e.RoadmapId == template.Id);

            return MapToDto(template, embedding is not null, embedding?.ComputedAt);
        }

        public async Task<RoadmapTemplateDto> UpdateTemplateAsync(int id, UpdateRoadmapTemplateDto dto)
        {
            var template = await _context.Roadmaps
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (template is null)
                throw new KeyNotFoundException($"Roadmap template with id {id} not found.");

            template.Track = dto.Track;
            template.Title = dto.Title;
            template.Description = dto.Description;
            template.OrderIndex = dto.OrderIndex;

            _context.RoadmapSteps.RemoveRange(template.Steps);

            template.Steps = dto.Steps.Select(s => new RoadmapStep
            {
                RoadmapId = id,
                Title = s.Title,
                Description = s.Description,
                Level = s.Level,
                Resources = JsonSerializer.Serialize(s.Resources),
                OrderIndex = s.OrderIndex
            }).ToList();

            await _context.SaveChangesAsync();

            await GenerateEmbeddingAsync(template);

            _logger.LogInformation("Admin updated roadmap template {Id} — {Title}", template.Id, template.Title);

            var embedding = await _context.RoadmapTemplateEmbeddings
                .FirstOrDefaultAsync(e => e.RoadmapId == id);

            return MapToDto(template, embedding is not null, embedding?.ComputedAt);
        }

        public async Task DeleteTemplateAsync(int id)
        {
            var template = await _context.Roadmaps.FindAsync(id);
            if (template is null)
                throw new KeyNotFoundException($"Roadmap template with id {id} not found.");

            _context.Roadmaps.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Admin deleted roadmap template {Id}", id);
        }

        public async Task<TestMatchResultDto> TestMatchAsync(int id, string? sampleCvText)
        {
            var template = await _context.Roadmaps.FindAsync(id);
            if (template is null)
                throw new KeyNotFoundException($"Roadmap template with id {id} not found.");

            string text;
            if (!string.IsNullOrWhiteSpace(sampleCvText))
            {
                text = sampleCvText;
            }
            else
            {
                var fullTemplate = await _context.Roadmaps
                    .Include(r => r.Steps)
                    .FirstAsync(r => r.Id == id);

                var stepsText = string.Join("\n    ", fullTemplate.Steps
                    .OrderBy(s => s.OrderIndex)
                    .Select(s => $"- {s.Title}: {s.Description} [Level: {s.Level}]"));

                text = $"Track: {fullTemplate.Track}\nTitle: {fullTemplate.Title}\nDescription: {fullTemplate.Description}\n\nSteps:\n    {stepsText}";
            }

            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            var allTemplates = await _context.Roadmaps
                .Join(
                    _context.RoadmapTemplateEmbeddings,
                    r => r.Id,
                    e => e.RoadmapId,
                    (r, e) => new { r.Id, r.Title, Embedding = e.Embedding }
                )
                .ToListAsync();

            double bestScore = 0;
            foreach (var t in allTemplates)
            {
                double score = ComputeCosineSimilarity(embedding, t.Embedding);
                if (score > bestScore)
                    bestScore = score;
            }

            return new TestMatchResultDto
            {
                TemplateId = id,
                TemplateName = template.Title,
                Score = bestScore
            };
        }

        private async Task GenerateEmbeddingAsync(Roadmap template)
        {
            try
            {
                var full = await _context.Roadmaps
                    .Include(r => r.Steps)
                    .FirstAsync(r => r.Id == template.Id);

                var stepsText = string.Join("\n    ", full.Steps
                    .OrderBy(s => s.OrderIndex)
                    .Select(s => $"- {s.Title}: {s.Description} [Level: {s.Level}]"));

                var combinedText = $"Track: {full.Track}\nTitle: {full.Title}\nDescription: {full.Description}\n\nSteps:\n    {stepsText}";

                var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(combinedText);

                var existing = await _context.RoadmapTemplateEmbeddings
                    .FirstOrDefaultAsync(e => e.RoadmapId == template.Id);

                if (existing is not null)
                {
                    existing.Embedding = embeddingVector;
                    existing.ComputedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.RoadmapTemplateEmbeddings.Add(new RoadmapTemplateEmbedding
                    {
                        RoadmapId = template.Id,
                        Embedding = embeddingVector,
                        ComputedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding for roadmap template {Id}", template.Id);
            }
        }

        private static double ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length || vectorA.Length == 0) return 0;
            double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }
            if (magnitudeA == 0 || magnitudeB == 0) return 0;
            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        private static RoadmapTemplateDto MapToDto(Roadmap r, bool hasEmbedding, DateTime? embeddingComputedAt) => new()
        {
            Id = r.Id,
            Track = r.Track,
            Title = r.Title,
            Description = r.Description,
            OrderIndex = r.OrderIndex,
            StepsCount = r.Steps.Count,
            HasEmbedding = hasEmbedding,
            EmbeddingComputedAt = embeddingComputedAt,
            Steps = r.Steps.OrderBy(s => s.OrderIndex).Select(s => new AdminRoadmapStepDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Level = s.Level,
                Resources = JsonSerializer.Deserialize<List<string>>(s.Resources) ?? new(),
                OrderIndex = s.OrderIndex
            }).ToList()
        };
    }
}
