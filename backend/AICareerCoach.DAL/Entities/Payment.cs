using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.DAL.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        public string? Status { get; set; }
        public int? UserSubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? IntentKey { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? TransactionKey { get; set; }

        public virtual UserSubscription? UserSubscription { get; set; }
    }
}
