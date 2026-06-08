using AICareerCoach.DAL.Entities;

namespace AICareerCoach.DAL.repository
{
    public interface IRoadmapRepository : IBaserepo<Roadmap>
    {
        Task<List<Roadmap>> GetAllWithStepsAsync(string? track);
        Task<Roadmap?> GetByIdWithStepsAsync(int id);
        Task<Roadmap> AddAsync(Roadmap roadmap);
    }
}
