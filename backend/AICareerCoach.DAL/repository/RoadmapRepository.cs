using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.repository
{
    public class RoadmapRepository : GenericRepo<Roadmap>, IRoadmapRepository
    {
        public RoadmapRepository(AICareerCoachDbContext context) : base(context) { }

        public async Task<List<Roadmap>> GetAllWithStepsAsync(string? track)
        {
            var query = dbset.Include(r => r.Steps.OrderBy(s => s.OrderIndex)).AsQueryable();

            if (!string.IsNullOrEmpty(track))
                query = query.Where(r => r.Track == track);

            return await query.OrderBy(r => r.OrderIndex).ToListAsync();
        }

        public async Task<Roadmap?> GetByIdWithStepsAsync(int id)
        {
            return await dbset
                .Include(r => r.Steps.OrderBy(s => s.OrderIndex))
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Roadmap> AddAsync(Roadmap roadmap)
        {
            await dbset.AddAsync(roadmap);
            await context.SaveChangesAsync();
            return roadmap;
        }
    }
}
