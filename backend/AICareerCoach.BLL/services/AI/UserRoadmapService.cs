using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AICareerCoach.BLL.Services.AI
{
    public class UserRoadmapService : IUserRoadmapService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IRoadmapTemplateStore _templateStore;
        private readonly IEmbeddingService _embeddingService;
        private readonly IRoadmapLlmService _llmService;
        private readonly ILogger<UserRoadmapService> _logger;

        public UserRoadmapService(
            AICareerCoachDbContext context,
            IRoadmapTemplateStore templateStore,
            IEmbeddingService embeddingService,
            IRoadmapLlmService llmService,
            ILogger<UserRoadmapService> logger)
        {
            _context = context;
            _templateStore = templateStore;
            _embeddingService = embeddingService;
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<UserRoadmapDto?> GetMyRoadmapAsync(string userId)
        {
            var roadmap = await _context.UserRoadmaps
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (roadmap is null) return null;

            return MapToDto(roadmap);
        }

        public async Task<UserRoadmapDto> RescanGapAnalysisAsync(string userId)
        {
            var roadmap = await _context.UserRoadmaps
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException(
                    "No roadmap found. Generate one first via POST /api/roadmap/generate.");

            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Please upload your CV first.");

            if (string.IsNullOrEmpty(cv.ExtractedData))
                throw new InvalidOperationException(
                    "CV text not extracted yet. Please request CV feedback first.");

            var template = await _templateStore.GetByTrackAsync(roadmap.TemplateTrack)
                ?? throw new InvalidOperationException(
                    $"Template track '{roadmap.TemplateTrack}' no longer exists.");

            var gapAnalysis = await _llmService.GenerateGapAnalysisAsync(
                cv.ExtractedData, roadmap.TargetRole, template);

            roadmap.GapAnalysisJson = JsonSerializer.Serialize(gapAnalysis);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Rescanned gap analysis for user {UserId} role {Role}.",
                userId, roadmap.TargetRole);

            return MapToDto(roadmap);
        }

        public async Task<UserRoadmapDto> AppendWeaknessStepsAsync(string userId, List<RoadmapStepResultDto> newSteps)
        {
            if (newSteps == null || newSteps.Count == 0)
                throw new InvalidOperationException("No steps to append.");

            var roadmap = await _context.UserRoadmaps
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException(
                    "No roadmap found. Generate one first via POST /api/roadmap/generate.");

            var existingSteps = JsonSerializer.Deserialize<List<RoadmapStepResultDto>>(roadmap.StepsJson) ?? new();
            int maxOrder = existingSteps.Count > 0 ? existingSteps.Max(s => s.Order) : 0;

            foreach (var step in newSteps)
            {
                step.Order = ++maxOrder;
                existingSteps.Add(step);
            }

            existingSteps = existingSteps.OrderBy(s => s.Order).ToList();

            roadmap.StepsJson = JsonSerializer.Serialize(existingSteps);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Appended {Count} weakness steps to roadmap for user {UserId}.",
                newSteps.Count, userId);

            return MapToDto(roadmap);
        }

        public async Task<UserRoadmapDto> GenerateRoadmapAsync(string userId, GenerateRoadmapRequestDto request)
        {
            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Please upload your CV first to generate a roadmap.");

            string cvText = cv.ExtractedData;

            if (string.IsNullOrEmpty(cvText))
                throw new InvalidOperationException("CV text not extracted yet. Please request CV feedback first.");

            string cvHash = ComputeMd5Hash(cvText);

            if (!request.ForceRegenerate)
            {
                var existing = await _context.UserRoadmaps
                    .Where(r => r.UserId == userId && r.CvHash == cvHash && r.TargetRole == request.TargetRole)
                    .FirstOrDefaultAsync();

                if (existing is not null)
                {
                    _logger.LogInformation("Roadmap already exists for user {UserId} role {Role}, returning cached.", userId, request.TargetRole);
                    return MapToDto(existing);
                }
            }

            Roadmap? template;
            double? matchScore = null;

            if (!string.IsNullOrEmpty(request.TemplateTrack))
            {
                template = await _templateStore.GetByTrackAsync(request.TemplateTrack);
                if (template is null)
                    throw new KeyNotFoundException($"Template track '{request.TemplateTrack}' not found.");
            }
            else
            {
                var cvEmbedding = await _embeddingService.GenerateEmbeddingAsync(cvText);
                var (autoTemplate, score) = await _templateStore.FindBestMatchAsync(cvEmbedding);
                template = autoTemplate;
                matchScore = score;
                if (template is null)
                    throw new InvalidOperationException("No matching roadmap template found.");
            }

            var (steps, gapAnalysis) = await _llmService.GenerateRoadmapAsync(cvText, request.TargetRole, template);

            var snapshot = new TemplateSnapshotDto
            {
                Id = template.Id,
                Track = template.Track,
                Title = template.Title,
                Description = template.Description,
                Steps = template.Steps.OrderBy(s => s.OrderIndex).Select(s => new RoadmapStepDto
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

            var userRoadmap = new UserRoadmap
            {
                UserId = userId,
                CvHash = cvHash,
                TargetRole = request.TargetRole,
                TemplateRoadmapId = template.Id,
                TemplateTrack = template.Track,
                TemplateSnapshotJson = JsonSerializer.Serialize(snapshot),
                StepsJson = JsonSerializer.Serialize(steps),
                GapAnalysisJson = JsonSerializer.Serialize(gapAnalysis),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoadmaps.Add(userRoadmap);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated new roadmap for user {UserId} role {Role}.", userId, request.TargetRole);

            return MapToDto(userRoadmap, matchScore, snapshot);
        }

        private static UserRoadmapDto MapToDto(
            UserRoadmap r,
            double? matchScore = null,
            TemplateSnapshotDto? templateSnapshot = null)
        {
            var steps = JsonSerializer.Deserialize<List<RoadmapStepResultDto>>(r.StepsJson) ?? new();
            var gapAnalysis = JsonSerializer.Deserialize<List<SkillsCategoryDto>>(r.GapAnalysisJson) ?? new();

            var snapshot = templateSnapshot
                ?? (string.IsNullOrEmpty(r.TemplateSnapshotJson)
                    ? null
                    : JsonSerializer.Deserialize<TemplateSnapshotDto>(r.TemplateSnapshotJson));

            return new UserRoadmapDto
            {
                Id = r.Id,
                TargetRole = r.TargetRole,
                TemplateTrack = r.TemplateTrack,
                Steps = steps,
                GapAnalysis = gapAnalysis,
                CreatedAt = r.CreatedAt,
                MatchScore = matchScore,
                TemplateSnapshot = snapshot
            };
        }

        private static string ComputeMd5Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = MD5.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
