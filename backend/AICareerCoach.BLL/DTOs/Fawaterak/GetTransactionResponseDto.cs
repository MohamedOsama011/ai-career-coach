using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class GetTransactionResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public TransactionDataDto? Data { get; set; }
    }

    public class TransactionDataDto
    {
        [JsonPropertyName("intent_key")]
        public string IntentKey { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }

        [JsonPropertyName("customer_email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("commission")]
        public decimal Commission { get; set; }

        [JsonPropertyName("transaction_created_at")]
        public string TransactionCreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("paid")]
        public int Paid { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("status_text")]
        public string StatusText { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("pay_load")]
        public object? PayLoad { get; set; }

        [JsonPropertyName("due_date")]
        public string DueDate { get; set; } = string.Empty;

        [JsonPropertyName("transaction_link")]
        public string TransactionLink { get; set; } = string.Empty;

        [JsonPropertyName("transaction_history")]
        public List<TransactionHistoryDto> TransactionHistory { get; set; } = new();
    }

    public class PaymentPayloadDto
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;
    }

    public class TransactionHistoryDto
    {
        [JsonPropertyName("method")]
        public PaymentMethodDto? Method { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;
    }
}
