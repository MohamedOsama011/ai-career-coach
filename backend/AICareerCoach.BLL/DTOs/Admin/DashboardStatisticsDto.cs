namespace AICareerCoach.BLL.DTOs.Admin
{
    public class DashboardStatisticsDto
    {
        public int Users { get; set; }
        public int Admins { get; set; }
        public int CVs { get; set; }
        public int Interviews { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveSubscriptions { get; set; }
    }
}
