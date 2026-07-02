using System.Text.Json.Serialization;

public class GetPaymentMethodsResponseDTO
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("vendorSettingsData")]
    public VendorSettingsData VendorSettingsData { get; set; }

    [JsonPropertyName("data")]
    public List<PaymentMethodDto> Data { get; set; }
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
    public string NameEn { get; set; }

    [JsonPropertyName("name_ar")]
    public string NameAr { get; set; }

    // Keep as string because the API returns "true"/"false"
    [JsonPropertyName("redirect")]
    public string Redirect { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }
}