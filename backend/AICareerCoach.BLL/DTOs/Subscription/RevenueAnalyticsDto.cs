namespace AICareerCoach.BLL.DTOs.Subscription
{
    public class RevenueAnalyticsDto
    {
        public RevenueSummary Summary { get; set; } = new();
        public List<MonthlyRevenuePoint> RevenueByMonth { get; set; } = new();
        public List<PlanBreakdown> SubscriptionsByPlan { get; set; } = new();
        public List<RecentTransaction> RecentTransactions { get; set; } = new();
    }

    public class RevenueSummary
    {
        public string Currency { get; set; } = "EGP";
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
        public decimal ChurnRate { get; set; }
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int PendingSubscribers { get; set; }
        public int CancelledSubscribers { get; set; }
        public int ExpiredSubscribers { get; set; }
    }

    public class MonthlyRevenuePoint
    {
        public string Month { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
    }

    public class PlanBreakdown
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public int ActiveCount { get; set; }
        public decimal Revenue { get; set; }
        public string Color { get; set; } = "#3B82F6";
    }

    public class RecentTransaction
    {
        public int PaymentId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
