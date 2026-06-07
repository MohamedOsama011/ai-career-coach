namespace AICareerCoach.DAL.Entities
{
    public class Roadmap
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }
    }
}
