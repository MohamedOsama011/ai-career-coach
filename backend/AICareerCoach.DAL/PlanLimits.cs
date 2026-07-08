using System.Text.Json;

namespace AICareerCoach.DAL
{
    public class PlanLimits
    {
        public int InterviewSessions { get; set; } = -1;
        public int RoadmapGenerations { get; set; } = -1;
        public int JobRecommendations { get; set; } = -1;
        public bool RoadmapRescan { get; set; } = true;

        public static PlanLimits FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new PlanLimits();
            try
            {
                return JsonSerializer.Deserialize<PlanLimits>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new PlanLimits();
            }
            catch
            {
                return new PlanLimits();
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static PlanLimits Unlimited => new()
        {
            InterviewSessions = -1,
            RoadmapGenerations = -1,
            JobRecommendations = -1,
            RoadmapRescan = true
        };
    }
}
