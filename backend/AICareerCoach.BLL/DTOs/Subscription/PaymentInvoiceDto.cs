namespace AICareerCoach.BLL.DTOs.Subscription
{
    public class PaymentInvoiceDto
    {
        public int PaymentId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public DateTime PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "Paid";
    }

    public class PagedPaymentHistoryDto
    {
        public List<PaymentInvoiceDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasNextPage { get; set; }
    }
}
