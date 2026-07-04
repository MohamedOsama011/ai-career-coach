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
        private readonly ILogger<FawaterakService> _logger;

        public FawaterakService(
            HttpClient httpClient,
            IConfiguration configuration,
            AICareerCoachDbContext context,
            IFawaterakTokenService tokenService,
            ILogger<FawaterakService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto dto)
        {
            var response = new CreatePaymentResponseDto();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == dto.UserId);
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

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var userSubscription = new UserSubscription
                {
                    UserId = dto.UserId,
                    SubscriptionId = int.Parse(dto.PlanId),
                    IsActive = false,
                    Status = "pending",
                };
                _context.UserSubscriptions.Add(userSubscription);
                await _context.SaveChangesAsync();

                var payment = new Payment
                {
                    UserSubscriptionId = userSubscription.Id,
                    Status = "pending",
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
                response.Data = paymentMethods;
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

        public async Task<object> ExecuteInvoiceAsync(string methodId, string userSubscriptionId)
        {
            var data = await _context.UserSubscriptions
                .Include(u => u.Payments)
                .Include(u => u.Subscription)
                .Include(u => u.User)
                .FirstOrDefaultAsync(x => x.Id.ToString() == userSubscriptionId);

            if (data == null)
                throw new KeyNotFoundException("User subscription not found.");

            var dto = new FawaterakPaymentRequestDto
            {
                Customer = new CustomerDto
                {
                    FirstName = data.User!.UserName ?? "",
                    LastName = data.User.FullName,
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
                    SuccessUrl = $"{_configuration["AppSettings:BaseUrl"]}/api/Fawaterak/successwebhook",
                    FailUrl = $"{_configuration["AppSettings:BaseUrl"]}/swagger/index.html",
                    PendingUrl = $"{_configuration["AppSettings:BaseUrl"]}/swagger/index.html",
                    WebhookUrl = $"{_configuration["AppSettings:BaseUrl"]}/swagger/index.html",
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

        public async Task<Generalresponse> HandleSuccessWebhookAsync(WebhookSuccessDto dto)
        {
            if (!VerifyWebhookHash(dto))
            {
                return new Generalresponse { Success = false, Data = "Invalid webhook hash." };
            }

            var payment = await _context.Payments
                .Include(x => x.UserSubscription)
                .FirstOrDefaultAsync(x => x.IntentKey == dto.TransactionKey);

            if (payment == null)
            {
                return new Generalresponse { Success = false, Data = "Payment not found." };
            }

            if (payment.Status == "paid")
            {
                return new Generalresponse { Success = true, Data = "Payment already processed." };
            }

            payment.Status = "paid";
            payment.PaymentMethod = dto.PaymentMethod;
            payment.TransactionId = dto.TransactionId.ToString();

            if (payment.UserSubscription != null)
            {
                payment.UserSubscription.IsActive = true;
                payment.UserSubscription.Status = "active";
                payment.UserSubscription.StartDate = DateTime.UtcNow;
                payment.UserSubscription.EndDate = DateTime.UtcNow.AddMonths(1);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} processed successfully via webhook", payment.Id);

            return new Generalresponse { Success = true, Data = dto };
        }

        private bool VerifyWebhookHash(WebhookSuccessDto dto)
        {
            var secretKey = _configuration["Fawaterak:HashApiKey"]
                ?? throw new InvalidOperationException("Fawaterak:HashApiKey is not configured.");

            var query = $"TransactionId={dto.TransactionId}&TransactionKey={dto.TransactionKey}&PaymentMethod={dto.PaymentMethod}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return generatedHash.Equals(dto.HashKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}
