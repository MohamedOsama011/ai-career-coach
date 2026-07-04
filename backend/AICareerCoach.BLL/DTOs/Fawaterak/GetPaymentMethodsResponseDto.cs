using System.Text.Json.Serialization;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class GetPaymentMethodsResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("vendorSettingsData")]
        public VendorSettingsData? VendorSettingsData { get; set; }

        [JsonPropertyName("data")]
        public List<PaymentMethodDto> Data { get; set; } = new();
    }

    public class VendorSettingsData
    {
        [JsonPropertyName("custome_iframe_title")]
        public string? CustomIframeTitle { get; set; }
    }

    public class PaymentMethodDto
    {
        [JsonPropertyName("payment_method_id")]
        public int PaymentMethodId { get; set; }

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; } = string.Empty;

        [JsonPropertyName("name_ar")]
        public string NameAr { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("redirect")]
        public string Redirect { get; set; } = string.Empty;

        [JsonPropertyName("logo")]
        public string Logo { get; set; } = string.Empty;
    }
}
