using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class WebhookSuccessDto
    {
        [JsonPropertyName("hashKey")]
        public string HashKey { get; set; } = string.Empty;

        [JsonPropertyName("invoice_key")]
        public string InvoiceKey { get; set; } = string.Empty;

        [JsonPropertyName("invoice_id")]
        public long InvoiceId { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("invoice_status")]
        public string InvoiceStatus { get; set; } = string.Empty;

        [JsonPropertyName("pay_load")]
        public object? PayLoad { get; set; }

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [JsonPropertyName("transaction_key")]
        public string TransactionKey { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("paidAmount")]
        public decimal PaidAmount { get; set; }

        [JsonPropertyName("paidCurrency")]
        public string PaidCurrency { get; set; } = string.Empty;

        [JsonPropertyName("paidAt")]
        public DateTime PaidAt { get; set; }

        [JsonPropertyName("customerData")]
        public CustomerDataDto? CustomerData { get; set; }

        public bool IsPaid =>
            InvoiceStatus?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true
            || Status?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true;

        public bool HasCancelFormat =>
            TransactionKey != string.Empty && TransactionId > 0;

        public bool HasInvoiceFormat =>
            InvoiceKey != string.Empty && InvoiceId > 0;
    }

    public class CustomerDataDto
    {
        [JsonPropertyName("customer_unique_id")]
        public string CustomerUniqueId { get; set; } = string.Empty;

        [JsonPropertyName("customer_first_name")]
        public string CustomerFirstName { get; set; } = string.Empty;

        [JsonPropertyName("customer_last_name")]
        public string CustomerLastName { get; set; } = string.Empty;

        [JsonPropertyName("customer_email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("customer_phone")]
        public string CustomerPhone { get; set; } = string.Empty;
    }

    public class WebhookPayloadDto
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customer_reference")]
        public string CustomerReference { get; set; } = string.Empty;
    }
}
