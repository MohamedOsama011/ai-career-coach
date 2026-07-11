using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class FawaterakTokenServiceTests
    {
        [Fact]
        public async Task GetAccessTokenAsync_ThrowsWhenConfigMissing()
        {
            var http = new HttpClient(new HttpClientHandler());
            var config = new ConfigurationBuilder().Build();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<FawaterakTokenService>>();

            var svc = new FawaterakTokenService(http, config, cache, logger.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetAccessTokenAsync());
        }

        [Fact]
        public async Task GetAccessTokenAsync_ReturnsTokenOnSuccess()
        {
            var tokenResponse = new { access_token = "tok-1", expires_in = 3600 };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
               });

            var http = new HttpClient(handlerMock.Object);

            var settings = new Dictionary<string, string>
            {
                { "Fawaterak:ClientId", "c" },
                { "Fawaterak:ClientSecret", "s" },
                { "Fawaterak:TokenUrl", "https://token" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<FawaterakTokenService>>();

            var svc = new FawaterakTokenService(http, config, cache, logger.Object);

            var token = await svc.GetAccessTokenAsync();
            token.Should().Be("tok-1");

            // cached
            var token2 = await svc.GetAccessTokenAsync();
            token2.Should().Be("tok-1");
        }
    }
}
