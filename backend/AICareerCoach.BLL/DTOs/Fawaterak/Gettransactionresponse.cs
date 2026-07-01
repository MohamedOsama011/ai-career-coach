using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class Gettransactionresponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public ExecutePaymentDataDto Data { get; set; } = new();
    }

    public class ExecutePaymentDataDto
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
        // أو DateTime إذا كانت Fawaterk ترجع دائمًا نفس التنسيق.

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
        public PaymentPayloadDto PayLoad { get; set; } = new();

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
        public PaymentMethodDto Method { get; set; } = new();

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

    public class PaymentMethodDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("logo")]
        public string Logo { get; set; } = string.Empty;
    }
}

