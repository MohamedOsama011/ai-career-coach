using AICareerCoach.DAL.Entities;

namespace AICareerCoach.DAL.repository
{
    public interface IJobRepository : IBaserepo<Job>
    {
        Task<(List<Job> Items, int TotalCount)> GetPagedAsync(string? search, string? location, decimal? minSalary, int page, int pageSize);
        Task<Job?> GetByIdAsync(int id);
        Task<Job> AddAsync(Job job);
    }
}
