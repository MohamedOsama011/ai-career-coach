namespace AICareerCoach.DAL.Models
{
    public class Interview
    {
        public int Id { get; set; }

        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        public int Score { get; set; }

        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }
    }
}
