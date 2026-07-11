using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class FawaterakService : IFawaterakService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly AICareerCoachDbContext _context;
        private readonly IFawaterakTokenService _tokenService;
        private readonly IUserSubscriptionService _userSubscriptionService;
        private readonly ILogger<FawaterakService> _logger;

        public FawaterakService(
            HttpClient httpClient,
            IConfiguration configuration,
            AICareerCoachDbContext context,
            IFawaterakTokenService tokenService,
            IUserSubscriptionService userSubscriptionService,
            ILogger<FawaterakService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _tokenService = tokenService;
            _userSubscriptionService = userSubscriptionService;
            _logger = logger;
        }

        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto dto, string userId)
        {
            var response = new CreatePaymentResponseDto();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var plan = await _context.Subscriptions.FirstOrDefaultAsync(x => x.Id.ToString() == dto.PlanId);

            if (plan == null)
            {
                response.Success = false;
                response.Data = "subscription not found";
                return response;
            }

            if (user == null)
            {
                response.Success = false;
                response.Data = "user not found";
                return response;
            }

            await _userSubscriptionService.RefreshExpiredSubscriptionsAsync(userId);

            var hasActive = await _context.UserSubscriptions
                .AnyAsync(us => us.UserId == userId
                             && us.IsActive
                             && us.EndDate > DateTime.UtcNow);
            if (hasActive)
            {
                _logger.LogWarning("CreatePayment blocked: User {UserId} already has an active subscription", userId);
                return new CreatePaymentResponseDto
                {
                    Success = false,
                    Data = "You already have an active subscription. Cancel it first to switch plans."
                };
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var userSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionId = int.Parse(dto.PlanId),
                    IsActive = false,
                    Status = SubscriptionStatus.Pending,
                    Quantity = 1,
                };
                _context.UserSubscriptions.Add(userSubscription);
                await _context.SaveChangesAsync();

                var payment = new Payment
                {
                    UserSubscriptionId = userSubscription.Id,
                    Status = PaymentStatus.Pending,
                    Amount = plan.Price,
                    InvoiceNumber = userSubscription.Id.ToString(),
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                response.UserSubscriptionId = userSubscription.Id.ToString();
            });

            var paymentMethods = await GetPaymentMethodsAsync();
            if (paymentMethods != null)
            {
                response.Success = true;
                response.Data = paymentMethods?.Data;
            }

            return response;
        }

        public async Task<GetPaymentMethodsResponseDto> GetPaymentMethodsAsync()
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_configuration["Fawaterak:BaseUrl"]}/api/v3/getTrPaymentmethods");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fawaterak get payment methods failed: {Result}", result);
                throw new Exception("Fawaterak error: " + result);
            }

            return JsonSerializer.Deserialize<GetPaymentMethodsResponseDto>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<ExecutePaymentResponseDto> ExecuteInvoiceAsync(string methodId, string userSubscriptionId, string userId)
        {
            var data = await _context.UserSubscriptions
                .Include(u => u.Payments)
                .Include(u => u.Subscription)
                .Include(u => u.User)
                .FirstOrDefaultAsync(x => x.Id.ToString() == userSubscriptionId);

            if (data == null)
                throw new KeyNotFoundException("User subscription not found.");

            if (data.UserId != userId)
            {
                _logger.LogWarning("ExecuteInvoice: User {UserId} attempted to execute invoice for subscription {SubId} owned by {OwnerId}",
                    userId, userSubscriptionId, data.UserId);
                throw new UnauthorizedAccessException("You do not own this subscription.");
            }

            var dto = new FawaterakPaymentRequestDto
            {
                Customer = new CustomerDto
                {
                    FirstName = SplitFirstName(data.User!.FullName, data.User.UserName ?? ""),
                    LastName = SplitLastName(data.User.FullName, data.User.UserName ?? ""),
                    Email = data.User.Email,
                    Phone = data.User.PhoneNumber,
                },
                CartTotal = data.Subscription!.Price,
                PaymentMethodId = int.Parse(methodId),
                Currency = "EGP",
                Lang = "en",
                DueDate = DateTime.UtcNow,
                MobileWalletNumber = "",
                TrNumber = "",
                SendSms = false,
                SendEmail = false,
                ListStyle = "horizontal",
                PayLoad = new PayloadDto
                {
                    OrderId = data.Payments?.FirstOrDefault()?.UserSubscriptionId.ToString() ?? "",
                    CustomerReference = data.UserId ?? "",
                },
                CartItems = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        Name = data.Subscription.Name ?? "",
                        Price = data.Subscription.Price,
                        Quantity = 1,
                    }
                },
                DiscountData = new DiscountDataDto { Type = "", Value = 0 },
                TaxData = new TaxDataDto { Title = "", Value = 0 },
                AuthAndCapture = 0,
                RedirectionUrls = new RedirectionUrlsDto
                {
                    SuccessUrl = $"{_configuration["AppSettings:FrontendBaseUrl"]}/my-subscriptions?payment=success",
                    FailUrl = $"{_configuration["AppSettings:FrontendBaseUrl"]}/my-subscriptions?payment=failed",
                    PendingUrl = $"{_configuration["AppSettings:FrontendBaseUrl"]}/my-subscriptions?payment=pending",
                    WebhookUrl = $"{_configuration["AppSettings:BaseUrl"]}/api/Fawaterak/success-webhook_json",
                },
            };

            return await ExecutePaymentAsync(dto);
        }

        public async Task<ExecutePaymentResponseDto> ExecutePaymentAsync(FawaterakPaymentRequestDto dto)
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();

            var requestBody = new
            {
                payment_method_id = dto.PaymentMethodId,
                cartTotal = dto.CartTotal,
                currency = dto.Currency,
                customer = new
                {
                    first_name = dto.Customer.FirstName,
                    last_name = dto.Customer.LastName,
                    email = dto.Customer.Email,
                    phone = dto.Customer.Phone,
                    address = dto.Customer.Address,
                },
                cartItems = dto.CartItems.Select(item => new
                {
                    name = item.Name,
                    price = item.Price,
                    quantity = item.Quantity,
                }).ToList(),
                pay_load = new
                {
                    order_id = dto.PayLoad.OrderId,
                    customer_reference = dto.PayLoad.CustomerReference,
                },
                redirectionUrls = new
                {
                    successUrl = dto.RedirectionUrls.SuccessUrl,
                    failUrl = dto.RedirectionUrls.FailUrl,
                    pendingUrl = dto.RedirectionUrls.PendingUrl,
                    webhookUrl = dto.RedirectionUrls.WebhookUrl,
                },
                sendEmail = dto.SendEmail,
                sendSMS = dto.SendSms,
                due_date = dto.DueDate,
                tr_number = dto.TrNumber,
                redirectOption = dto.RedirectionUrls.SuccessUrl != null,
                authAndCapture = dto.AuthAndCapture,
                taxData = new { title = dto.TaxData?.Title ?? "", value = dto.TaxData?.Value ?? 0 },
                discountData = new { type = dto.DiscountData?.Type ?? "", value = dto.DiscountData?.Value ?? 0 },
                list_style = dto.ListStyle,
                lang = dto.Lang,
                mobileWalletNumber = dto.MobileWalletNumber ?? "",
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Fawaterak:BaseUrl"]}/api/v3/createTransaction");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fawaterak execute payment failed: {Result}", result);
                throw new Exception("Fawaterak error: " + result);
            }

            var responseObject = JsonSerializer.Deserialize<ExecutePaymentResponseDto>(
                result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (responseObject == null)
                throw new Exception("Failed to deserialize Fawaterak response.");

            if (responseObject.Data != null)
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.UserSubscriptionId.ToString() == dto.PayLoad.OrderId);
                if (payment != null)
                {
                    payment.IntentKey = responseObject.Data.IntentKey;
                    await _context.SaveChangesAsync();
                }
            }

            return responseObject;
        }

        public async Task<GetTransactionResponseDto> GetTransactionDataAsync(GetTransactionRequestDto dto)
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();

            var requestBody = new { intent_key = dto.IntentKey };
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_configuration["Fawaterak:BaseUrl"]}/api/v3/getTransactionData");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fawaterak get transaction data failed: {Result}", result);
                throw new Exception("Fawaterak error: " + result);
            }

            return JsonSerializer.Deserialize<GetTransactionResponseDto>(
                result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<GeneralResponse<WebhookSuccessDto>> HandleSuccessWebhookAsync(WebhookSuccessDto dto)
        {
            var matchedBy = "";

            if (dto.HasInvoiceFormat)
            {
                if (!VerifyInvoiceWebhookHash(dto))
                {
                    _logger.LogWarning("Invalid webhook hash for InvoiceId={InvoiceId}", dto.InvoiceId);
                    return new GeneralResponse<WebhookSuccessDto> { Success = false };
                }
                matchedBy = "invoice";
            }
            else if (dto.HasCancelFormat)
            {
                if (!VerifyCancelWebhookHash(dto))
                {
                    _logger.LogWarning("Invalid webhook hash for TransactionId={TransactionId}", dto.TransactionId);
                    return new GeneralResponse<WebhookSuccessDto> { Success = false };
                }
                matchedBy = "cancel";
            }
            else
            {
                _logger.LogWarning("Webhook: unrecognized payload format — no invoice or transaction fields");
                return new GeneralResponse<WebhookSuccessDto> { Success = false };
            }

            if (!dto.IsPaid)
            {
                _logger.LogWarning("Webhook: non-paid status ({Status}/{InvoiceStatus}) — ignoring",
                    dto.Status, dto.InvoiceStatus);
                return new GeneralResponse<WebhookSuccessDto> { Success = false };
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                Payment? payment = null;

                if (dto.PayLoad is JsonElement jsonElement
                    && jsonElement.TryGetProperty("order_id", out var orderIdProp))
                {
                    var orderId = orderIdProp.GetString();
                    if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out var subId))
                    {
                        payment = await _context.Payments
                            .Include(x => x.UserSubscription)
                            .FirstOrDefaultAsync(x => x.UserSubscriptionId == subId);
                        if (payment != null)
                            _logger.LogInformation("Webhook: matched payment {PaymentId} via pay_load.order_id={OrderId}", payment.Id, orderId);
                    }
                }

                if (payment == null && matchedBy == "invoice" && !string.IsNullOrEmpty(dto.InvoiceKey))
                {
                    _logger.LogWarning("Webhook: pay_load.order_id lookup failed, using IntentKey match for InvoiceKey={InvoiceKey}", dto.InvoiceKey);
                    return new GeneralResponse<WebhookSuccessDto> { Success = false, Data = dto };
                }

                if (payment == null)
                {
                    _logger.LogWarning("Webhook: Payment not found for any matching strategy");
                    return new GeneralResponse<WebhookSuccessDto> { Success = false };
                }

                if (payment.Status == PaymentStatus.Paid)
                {
                    _logger.LogInformation("Webhook: Payment {PaymentId} already processed", payment.Id);
                    return new GeneralResponse<WebhookSuccessDto> { Success = true, Data = dto };
                }

                payment.Status = PaymentStatus.Paid;
                payment.PaymentMethod = dto.PaymentMethod;
                payment.TransactionId = dto.HasInvoiceFormat
                    ? dto.InvoiceId.ToString()
                    : dto.TransactionId.ToString();

                if (payment.UserSubscription != null)
                {
                    payment.UserSubscription.IsActive = true;
                    payment.UserSubscription.Status = SubscriptionStatus.Active;
                    payment.UserSubscription.StartDate = DateTime.UtcNow;

                    var subscription = await _context.Subscriptions
                        .FirstOrDefaultAsync(s => s.Id == payment.UserSubscription.SubscriptionId);
                    var durationMonths = subscription?.DurationMonths ?? 1;
                    payment.UserSubscription.EndDate = DateTime.UtcNow.AddMonths(durationMonths);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment {PaymentId} processed successfully via webhook (duration={DurationMonths}mo, matchedBy={MatchedBy})",
                    payment.Id,
                    payment.UserSubscription?.Subscription?.DurationMonths ?? 1,
                    matchedBy);

                return new GeneralResponse<WebhookSuccessDto> { Success = true, Data = dto };
            });
        }

        private bool VerifyInvoiceWebhookHash(WebhookSuccessDto dto)
        {
            var secretKey = _configuration["Fawaterak:HashApiKey"]
                ?? throw new InvalidOperationException("Fawaterak:HashApiKey is not configured.");

            var query = $"InvoiceId={dto.InvoiceId}&InvoiceKey={dto.InvoiceKey}&PaymentMethod={dto.PaymentMethod}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return generatedHash.Equals(dto.HashKey, StringComparison.OrdinalIgnoreCase);
        }

        private bool VerifyCancelWebhookHash(WebhookSuccessDto dto)
        {
            var secretKey = _configuration["Fawaterak:HashApiKey"]
                ?? throw new InvalidOperationException("Fawaterak:HashApiKey is not configured.");

            var query = $"TransactionId={dto.TransactionId}&TransactionKey={dto.TransactionKey}&PaymentMethod={dto.PaymentMethod}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return generatedHash.Equals(dto.HashKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string SplitFirstName(string? fullName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fallback;
            var trimmed = fullName.Trim();
            var spaceIndex = trimmed.IndexOf(' ');
            return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
        }

        private static string SplitLastName(string? fullName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fallback;
            var trimmed = fullName.Trim();
            var spaceIndex = trimmed.IndexOf(' ');
            return spaceIndex > 0 ? trimmed[(spaceIndex + 1)..] : trimmed;
        }
    }
}
