using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IRoadmapTemplateStore
    {
        Task<List<Roadmap>> GetAllAsync();
        Task<Roadmap?> GetByIdAsync(int id);
        Task<Roadmap?> GetByTrackAsync(string track);
        Task<Roadmap?> FindBestMatchAsync(float[] cvEmbedding);
    }
}
