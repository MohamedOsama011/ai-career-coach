using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class ExecutePaymentResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public FawaterakPaymentDataDto? Data { get; set; }
    }

    public class FawaterakPaymentDataDto
    {
        [JsonPropertyName("intent_key")]
        public string IntentKey { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("Payment_Data")]
        public PaymentRedirectDto? PaymentData { get; set; }
    }

    public class PaymentRedirectDto
    {
        [JsonPropertyName("RedirectTo")]
        public string RedirectTo { get; set; } = string.Empty;
    }
}
