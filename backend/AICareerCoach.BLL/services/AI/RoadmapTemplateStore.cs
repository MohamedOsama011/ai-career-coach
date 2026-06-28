using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.BLL.Services.AI
{
    public class RoadmapTemplateStore : IRoadmapTemplateStore
    {
        private readonly AICareerCoachDbContext _context;

        public RoadmapTemplateStore(AICareerCoachDbContext context)
        {
            _context = context;
        }

        public async Task<List<Roadmap>> GetAllAsync()
        {
            return await _context.Roadmaps
                .Include(r => r.Steps)
                .ToListAsync();
        }

        public async Task<Roadmap?> GetByIdAsync(int id)
        {
            return await _context.Roadmaps
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Roadmap?> GetByTrackAsync(string track)
        {
            return await _context.Roadmaps
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Track == track);
        }

        public async Task<(Roadmap? Template, double Score)> FindBestMatchAsync(float[] cvEmbedding)
        {
            var templates = await _context.Roadmaps
                .Include(r => r.Steps)
                .Join(
                    _context.RoadmapTemplateEmbeddings,
                    r => r.Id,
                    e => e.RoadmapId,
                    (r, e) => new { Roadmap = r, Embedding = e.Embedding }
                )
                .ToListAsync();

            if (!templates.Any()) return (null, 0);

            double bestScore = -1;
            Roadmap? bestMatch = null;

            foreach (var t in templates)
            {
                double score = ComputeCosineSimilarity(cvEmbedding, t.Embedding);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = t.Roadmap;
                }
            }

            return (bestMatch, bestScore);
        }

        private static double ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0;

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
    }
}
