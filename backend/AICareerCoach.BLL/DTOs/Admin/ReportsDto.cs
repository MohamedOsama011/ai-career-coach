namespace AICareerCoach.BLL.DTOs.Admin
{
    public class ReportsDto
    {
        public List<MonthlyPoint> UsersOverTime { get; set; } = new();
        public List<DailyPoint> InterviewsPerDay { get; set; } = new();
        public List<SimpleCount> TopRequestedRoles { get; set; } = new();
        public List<SimpleCount> PopularSkills { get; set; } = new();
    }

    public class MonthlyPoint
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DailyPoint
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SimpleCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
