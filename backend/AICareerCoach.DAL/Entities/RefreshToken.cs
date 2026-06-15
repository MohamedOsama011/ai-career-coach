using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime Expirydate { get; set; }

        public string Token { get; set; } = string.Empty;

        public string Userid { get; set; } = string.Empty;

        public virtual User? User { get; set; }
    }
}
