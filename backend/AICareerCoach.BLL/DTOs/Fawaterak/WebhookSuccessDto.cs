using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class WebhookSuccessDto
    {
        [JsonPropertyName("transaction_key")]
        public string TransactionKey { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("pay_load")]
        public object? PayLoad { get; set; }

        [JsonPropertyName("paidAmount")]
        public decimal PaidAmount { get; set; }

        [JsonPropertyName("paidCurrency")]
        public string PaidCurrency { get; set; } = string.Empty;

        [JsonPropertyName("paidAt")]
        public DateTime PaidAt { get; set; }

        [JsonPropertyName("customerData")]
        public CustomerDataDto? CustomerData { get; set; }

        [JsonPropertyName("hashKey")]
        public string HashKey { get; set; } = string.Empty;
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
}
