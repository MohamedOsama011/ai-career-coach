namespace AICareerCoach.BLL.DTOs.Subscription
{
    public class SubscriptionGateStatusDto
    {
        public bool HasActiveSub { get; set; }
        public string? PlanName { get; set; }
        public DateTime? EndDate { get; set; }
        public GateFeaturesDto Features { get; set; } = new();
    }

    public class GateFeaturesDto
    {
        public GateFeatureStatus Interview { get; set; } = new();
        public GateFeatureStatus Roadmap { get; set; } = new();
        public GateFeatureStatus Jobs { get; set; } = new();
    }

    public class GateFeatureStatus
    {
        public int Used { get; set; }
        public int Limit { get; set; }
        public bool Allowed { get; set; }
    }
}
