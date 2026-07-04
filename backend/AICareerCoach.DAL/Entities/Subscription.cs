using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.DAL.Entities
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }

        public virtual ICollection<UserSubscription>? UserSubscriptions { get; set; } = new HashSet<UserSubscription>();
    }
}
