namespace AICareerCoach.BLL.DTOs
{
    public class SubscriptionDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMonths { get; set; } = 1;
    }
}
