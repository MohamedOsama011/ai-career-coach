using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs
{
    public class FawaterakDto
    {
      
        [JsonPropertyName("payment_method_id")]
        public int PaymentMethodId { get; set; }

        [JsonPropertyName("cartTotal")]
        public decimal CartTotal { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("customer")]
        public CustomerDto Customer { get; set; } = new();

        [JsonPropertyName("cartItems")]
        public List<CartItemDto> CartItems { get; set; } = new();

        [JsonPropertyName("pay_load")]
        public PayloadDto PayLoad { get; set; } = new();


        [JsonPropertyName("authAndCapture")]
        public int? AuthAndCapture { get; set; }


        [JsonPropertyName("redirectionUrls")]
        public RedirectionUrlsDto RedirectionUrls { get; set; } = new();

        [JsonPropertyName("sendEmail")]
        public bool SendEmail { get; set; }

        [JsonPropertyName("sendSMS")]
        public bool SendSms { get; set; }

        [JsonPropertyName("due_date")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("tr_number")]
        public string TrNumber { get; set; } = string.Empty;

        [JsonPropertyName("redirectOption")]
        public bool RedirectOption { get; set; }

        [JsonPropertyName("taxData")]
        public TaxDataDto? TaxData { get; set; } = new();

        [JsonPropertyName("discountData")]
        public DiscountDataDto? DiscountData { get; set; } = new();

        [JsonPropertyName("list_style")]
        public string? ListStyle { get; set; } = string.Empty;

        [JsonPropertyName("lang")]
        public string Lang { get; set; } = string.Empty;

        [JsonPropertyName("mobileWalletNumber")]
        public string? MobileWalletNumber { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string? Address { get; set; } = string.Empty;

        [JsonPropertyName("customer_number")]
        public string? CustomerNumber { get; set; } = string.Empty;

        [JsonPropertyName("customer_unique_id")]
        public string CustomerUniqueId { get; set; } = string.Empty;
    }

    public class CartItemDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class PayloadDto
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("customer_reference")]
        public string CustomerReference { get; set; }
    }

    public class RedirectionUrlsDto
    {
        [JsonPropertyName("successUrl")]
        public string SuccessUrl { get; set; } = string.Empty;

        [JsonPropertyName("failUrl")]
        public string FailUrl { get; set; } = string.Empty;

        [JsonPropertyName("pendingUrl")]
        public string PendingUrl { get; set; } = string.Empty;

        [JsonPropertyName("webhookUrl")]
        public string WebhookUrl { get; set; } = string.Empty;
    }

    public class TaxDataDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }

    public class DiscountDataDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }
}
