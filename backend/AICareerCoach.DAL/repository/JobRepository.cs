using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.repository
{
    public class JobRepository : GenericRepo<Job>, IJobRepository
    {
        public JobRepository(AICareerCoachDbContext context) : base(context) { }

        public async Task<(List<Job> Items, int TotalCount)> GetPagedAsync(string? search, string? location, decimal? minSalary, int page, int pageSize)
        {
            var query = dbset.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j =>
                    j.Title.Contains(search) ||
                    j.Company.Contains(search) ||
                    j.Description.Contains(search));

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.Location.Contains(location));

            if (minSalary.HasValue)
                query = query.Where(j => j.Salary >= minSalary.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.PostedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Job?> GetByIdAsync(int id)
        {
            return await dbset.FindAsync(id);
        }

        public async Task<Job> AddAsync(Job job)
        {
            await dbset.AddAsync(job);
            await context.SaveChangesAsync();
            return job;
        }
    }
}
