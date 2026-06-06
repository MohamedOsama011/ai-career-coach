namespace AICareerCoach.DAL.Models
{
    public class mockInterview
    {
        public int Id { get; set; }

        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public int Score { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}