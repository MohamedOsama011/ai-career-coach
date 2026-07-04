using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? SubscriptionId { get; set; }
        public bool IsActive { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; } = "pending";

        public virtual User? User { get; set; }
        public virtual Subscription? Subscription { get; set; }
        public virtual ICollection<Payment>? Payments { get; set; } = new List<Payment>();
    }
}
