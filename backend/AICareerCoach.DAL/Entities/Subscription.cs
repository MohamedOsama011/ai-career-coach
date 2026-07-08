using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.DAL.Entities
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int DurationMonths { get; set; } = 1;
        public string? LimitsJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<UserSubscription>? UserSubscriptions { get; set; } = new HashSet<UserSubscription>();
    }
}
