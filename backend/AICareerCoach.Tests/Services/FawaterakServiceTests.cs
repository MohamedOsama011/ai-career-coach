using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;

using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class FawaterakServiceTests
    {
            using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
    {
        public class PaymentServiceTests
        {
            private static AICareerCoachDbContext CreateInMemoryContext(string dbName = null)
            {
                var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                    .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                    .Options;

                return new AICareerCoachDbContext(options);
            }

            private static HttpClient CreateHttpClientReturning(string content, HttpStatusCode status = HttpStatusCode.OK)
            {
                var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
                handlerMock
                   .Protected()
                   .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                   .ReturnsAsync(new HttpResponseMessage()
                   {
                       StatusCode = status,
                       Content = new StringContent(content, Encoding.UTF8, "application/json")
                   })
                   .Verifiable();

                return new HttpClient(handlerMock.Object)
                {
                    BaseAddress = new Uri("http://test")
                };
            }

            private static AICareerCoach.BLL.Services.FawaterakService CreateService(HttpClient http, IConfiguration config, AICareerCoachDbContext context, Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService> tokenService = null, Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService> userSubService = null)
            {
                tokenService ??= new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                userSubService ??= new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>();
                var logger = new Mock<ILogger<AICareerCoach.BLL.Services.FawaterakService>>();
                return new AICareerCoach.BLL.Services.FawaterakService(http, config, context, tokenService.Object, userSubService.Object, logger.Object);
            }

            [Fact]
            public async Task CreatePaymentAsync_ReturnsError_WhenPlanNotFound()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                { "Fawaterak:BaseUrl", "http://fake" }, { "Fawaterak:HashApiKey", "key" }, { "AppSettings:FrontendBaseUrl", "http://f" }, { "AppSettings:BaseUrl", "http://b" }
            }).Build();
                var http = CreateHttpClientReturning("{}");
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                var userSubService = new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>();
                var svc = CreateService(http, config, context, tokenService, userSubService);
                var dto = new CreatePaymentRequestDto { PlanId = "999" };

                // Act
                var res = await svc.CreatePaymentAsync(dto, "u1");

                // Assert
                // Arrange: no subscription seeded -> Act: call CreatePaymentAsync -> Assert: subscription not found
                Assert.False(res.Success);
                Assert.Equal("subscription not found", res.Data);
            }

            [Fact]
            public async Task CreatePaymentAsync_ReturnsError_WhenUserNotFound()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var sub = new Subscription { Id = 1, Name = "P", Price = 10 };
                context.Subscriptions.Add(sub);
                context.SaveChanges();

                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                { "Fawaterak:BaseUrl", "http://fake" }, { "Fawaterak:HashApiKey", "key" }, { "AppSettings:FrontendBaseUrl", "http://f" }, { "AppSettings:BaseUrl", "http://b" }
            }).Build();
                var http = CreateHttpClientReturning("{}");
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                var userSubService = new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>();
                var svc = CreateService(http, config, context, tokenService, userSubService);
                var dto = new CreatePaymentRequestDto { PlanId = sub.Id.ToString() };

                // Act
                var res = await svc.CreatePaymentAsync(dto, "missingUser");

                // Assert
                Assert.False(res.Success);
                Assert.Equal("user not found", res.Data);
            }

            [Fact]
            public async Task CreatePaymentAsync_Blocked_WhenUserHasActiveSubscription()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var user = new DAL.Models.User { Id = "u1", Email = "a@b.com", UserName = "u1" };
                var sub = new Subscription { Id = 2, Name = "P2", Price = 20 };
                context.Users.Add(user);
                context.Subscriptions.Add(sub);
                context.UserSubscriptions.Add(new UserSubscription { UserId = user.Id, SubscriptionId = sub.Id, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10) });
                context.SaveChanges();

                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                { "Fawaterak:BaseUrl", "http://fake" }, { "Fawaterak:HashApiKey", "key" }, { "AppSettings:FrontendBaseUrl", "http://f" }, { "AppSettings:BaseUrl", "http://b" }
            }).Build();
                var http = CreateHttpClientReturning("{}");
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                var userSubService = new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>();
                var svc = CreateService(http, config, context, tokenService, userSubService);

                var dto = new CreatePaymentRequestDto { PlanId = sub.Id.ToString() };

                // Act
                var res = await svc.CreatePaymentAsync(dto, user.Id);

                // Assert
                Assert.False(res.Success);
                Assert.Contains("active subscription", res.Data.ToString());
            }

            [Fact]
            public async Task CreatePaymentAsync_Success_ReturnsPaymentMethods()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var user = new DAL.Models.User { Id = "u2", Email = "u2@e.com", UserName = "u2" };
                var sub = new Subscription { Id = 3, Name = "P3", Price = 30 };
                context.Users.Add(user);
                context.Subscriptions.Add(sub);
                context.SaveChanges();

                var methodsDto = new AICareerCoach.BLL.DTOs.Fawaterak.GetPaymentMethodsResponseDto
                {
                    Status = "success",
                    Data = new List<AICareerCoach.BLL.DTOs.Fawaterak.PaymentMethodDto>
                {
                    new AICareerCoach.BLL.DTOs.Fawaterak.PaymentMethodDto { PaymentMethodId = 1, Name = "pm", NameEn = "pm", NameAr = "", Redirect = "", Logo = "" }
                }
                };
                var json = JsonSerializer.Serialize(methodsDto);

                var http = CreateHttpClientReturning(json, HttpStatusCode.OK);
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                tokenService.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("tok");
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                { "Fawaterak:BaseUrl", "http://fake" }, { "Fawaterak:HashApiKey", "key" }, { "AppSettings:FrontendBaseUrl", "http://f" }, { "AppSettings:BaseUrl", "http://b" }
            }).Build();

                var userSubService = new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>();
                var svc = CreateService(http, config, context, tokenService, userSubService);

                var dto = new CreatePaymentRequestDto { PlanId = sub.Id.ToString() };

                // Act
                var res = await svc.CreatePaymentAsync(dto, user.Id);

                // Assert
                Assert.Equal(true, res.Success);
                Assert.NotNull(res.UserSubscriptionId);
                Assert.Equal(methodsDto.Data, res.Data);
            }

            [Fact]
            public async Task GetPaymentMethodsAsync_Throws_OnHttpError()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                tokenService.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("tok");
                var http = CreateHttpClientReturning("error", HttpStatusCode.InternalServerError);
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:BaseUrl", "http://fake" } }).Build();
                var svc = CreateService(http, config, context, tokenService, new Mock<AICareerCoach.BLL.Interfaces.IUserSubscriptionService>());

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => svc.GetPaymentMethodsAsync());
            }

            [Fact]
            public async Task ExecuteInvoiceAsync_Throws_WhenSubscriptionNotFound()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var http = CreateHttpClientReturning("{}", HttpStatusCode.OK);
                var svc = CreateService(http, new ConfigurationBuilder().AddInMemoryCollection().Build(), context);

                // Act & Assert
                await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.ExecuteInvoiceAsync("1", "sub1", "u1"));
            }

            [Fact]
            public async Task ExecuteInvoiceAsync_Throws_WhenUserMismatch()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var user = new DAL.Models.User { Id = "owner", Email = "o@e.com" };
                var sub = new Subscription { Id = 5, Name = "P5", Price = 50 };
                var us = new UserSubscription { Id = 10, UserId = "owner", SubscriptionId = sub.Id };
                context.Users.Add(user);
                context.Subscriptions.Add(sub);
                context.UserSubscriptions.Add(us);
                context.SaveChanges();

                var http = CreateHttpClientReturning("{}", HttpStatusCode.OK);
                var svc = CreateService(http, new ConfigurationBuilder().AddInMemoryCollection().Build(), context);

                // Act & Assert: caller is different user
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.ExecuteInvoiceAsync("1", us.Id.ToString(), "attacker"));
            }

            [Fact]
            public async Task ExecutePaymentAsync_Throws_OnApiError()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                tokenService.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("tok");
                var http = CreateHttpClientReturning("err", HttpStatusCode.InternalServerError);
                var svc = CreateService(http, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:BaseUrl", "http://fake" } }).Build(), context, tokenService);

                var dto = new FawaterakPaymentRequestDto { PaymentMethodId = 1, CartTotal = 10, PayLoad = new PayloadDto { OrderId = "99" } };

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => svc.ExecutePaymentAsync(dto));
            }

            [Fact]
            public async Task ExecutePaymentAsync_Success_UpdatesPaymentIntentKey()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var sub = new Subscription { Id = 7, Name = "P7", Price = 70 };
                context.Subscriptions.Add(sub);
                var us = new UserSubscription { Id = 21, UserId = "u3", SubscriptionId = sub.Id };
                context.UserSubscriptions.Add(us);
                var payment = new Payment { Id = 33, UserSubscriptionId = us.Id, Status = PaymentStatus.Pending, Amount = sub.Price };
                context.Payments.Add(payment);
                context.SaveChanges();

                var responseDto = new AICareerCoach.BLL.DTOs.Fawaterak.ExecutePaymentResponseDto
                {
                    Status = "success",
                    Data = new AICareerCoach.BLL.DTOs.Fawaterak.FawaterakPaymentDataDto { IntentKey = "intent-123" }
                };
                var json = JsonSerializer.Serialize(responseDto);

                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                tokenService.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("tok");
                var http = CreateHttpClientReturning(json, HttpStatusCode.OK);
                var svc = CreateService(http, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:BaseUrl", "http://fake" } }).Build(), context, tokenService);

                var dto = new FawaterakPaymentRequestDto { PaymentMethodId = 1, CartTotal = 70, PayLoad = new PayloadDto { OrderId = us.Id.ToString() } };

                // Act
                var res = await svc.ExecutePaymentAsync(dto);

                // Assert
                Assert.Equal("success", res.Status); // verified
                var p = await context.Payments.FindAsync(payment.Id);
                Assert.Equal("intent-123", p!.IntentKey);
            }

            [Fact]
            public async Task GetTransactionDataAsync_Throws_OnApiError()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenService = new Mock<AICareerCoach.BLL.Interfaces.IFawaterakTokenService>();
                tokenService.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("tok");
                var http = CreateHttpClientReturning("err", HttpStatusCode.InternalServerError);
                var svc = CreateService(http, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:BaseUrl", "http://fake" } }).Build(), context, tokenService);

                var dto = new GetTransactionRequestDto { IntentKey = "k" };

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() => svc.GetTransactionDataAsync(dto));
            }

            [Fact]
            public async Task HandleSuccessWebhookAsync_InvalidHash_ReturnsFalse()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var dto = new AICareerCoach.BLL.DTOs.Fawaterak.WebhookSuccessDto { TransactionId = 1, TransactionKey = "tk", PaymentMethod = "1", Status = "s", PaidAt = DateTime.UtcNow, PaidAmount = 0, PaidCurrency = "EGP" };
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:HashApiKey", "secret" } }).Build();
                var http = CreateHttpClientReturning("{}", HttpStatusCode.OK);
                var svc = CreateService(http, config, context);

                // Act
                var res = await svc.HandleSuccessWebhookAsync(dto);

                // Assert
                Assert.False(res.Success);
            }

            [Fact]
            public async Task HandleSuccessWebhookAsync_ProcessesPayment_Succeeds()
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var user = new DAL.Models.User { Id = "u9", UserName = "u9", Email = "u9@e.com" };
                var sub = new Subscription { Id = 11, Name = "P11", Price = 110, DurationMonths = 1 };
                context.Users.Add(user);
                context.Subscriptions.Add(sub);
                var us = new UserSubscription { Id = 55, UserId = user.Id, SubscriptionId = sub.Id, IsActive = false, Status = SubscriptionStatus.Pending };
                context.UserSubscriptions.Add(us);
                var payment = new Payment { Id = 77, UserSubscriptionId = us.Id, Status = PaymentStatus.Pending, IntentKey = "ikey-77" };
                context.Payments.Add(payment);
                context.SaveChanges();

                var dto = new AICareerCoach.BLL.DTOs.Fawaterak.WebhookSuccessDto { TransactionId = 777, TransactionKey = "ikey-77", PaymentMethod = "2" };
                // compute valid hash
                var secret = "mysecret";
                var query = $"TransactionId={dto.TransactionId}&TransactionKey={dto.TransactionKey}&PaymentMethod={dto.PaymentMethod}";
                using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
                dto.HashKey = Convert.ToHexString(hashBytes).ToLowerInvariant();

                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "Fawaterak:HashApiKey", secret } }).Build();
                var http = CreateHttpClientReturning("{}", HttpStatusCode.OK);
                var svc = CreateService(http, config, context);

                // Act
                var res = await svc.HandleSuccessWebhookAsync(dto);

                // Assert
                Assert.True(res.Success);
                var p = await context.Payments.FindAsync(payment.Id);
                Assert.Equal(PaymentStatus.Paid, p!.Status);
                var updatedUs = await context.UserSubscriptions.FindAsync(us.Id);
                Assert.True(updatedUs!.IsActive);
                Assert.Equal(SubscriptionStatus.Active, updatedUs.Status);
            }
        }
    }

}



