namespace AICareerCoach.BLL.Interfaces
{
    public enum Feature
    {
        InterviewSession,
        RoadmapGeneration,
        RoadmapRescan,
        JobRecommendations
    }

    public record GateResult(bool Allowed, string Reason, int Used, int Limit);

    public interface ISubscriptionGateService
    {
        Task<bool> HasActiveSubscriptionAsync(string userId);
        Task<GateResult> CheckAccessAsync(string userId, Feature feature);
    }
}
