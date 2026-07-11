using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class UserSubscriptionServiceTests
    {
        private readonly AICareerCoachDbContext _context;
        private readonly UserSubscriptionService _service;

        public UserSubscriptionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            _context = new AICareerCoachDbContext(options);
            var logger = new Mock<ILogger<UserSubscriptionService>>();
            _service = new UserSubscriptionService(_context, logger.Object);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_ReturnsList()
        {
            _context.UserSubscriptions.Add(new UserSubscription { Id = 1, UserId = "u1" });
            _context.SaveChanges();

            var res = await _service.GetAllByUserIdAsync("u1");
            res.Success.Should().BeTrue();
            res.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenNotFound_ReturnsNotFound()
        {
            var res = await _service.CancelSubscriptionAsync(999, "u1");
            res.Success.Should().BeFalse();
            res.Data.Should().Be("subscription not found");
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenUnauthorized_ReturnsUnauthorized()
        {
            var us = new UserSubscription { Id = 2, UserId = "owner", IsActive = true };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.CancelSubscriptionAsync(2, "other");
            res.Success.Should().BeFalse();
            res.Data.Should().Be("unauthorized");
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenSuccess_ReturnsCancelled()
        {
            var us = new UserSubscription { Id = 3, UserId = "u1", IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), Status = SubscriptionStatus.Active };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.CancelSubscriptionAsync(3, "u1");
            res.Success.Should().BeTrue();
            res.Data.Should().Be("cancelled successfully");
        }

        [Fact]
        public async Task RefreshExpiredSubscriptionsAsync_WhenNone_ReturnsZero()
        {
            var count = await _service.RefreshExpiredSubscriptionsAsync("u-none");
            count.Should().Be(0);
        }

        [Fact]
        public async Task RefreshExpiredSubscriptionsAsync_WhenExpired_ReturnsCount()
        {
            var us = new UserSubscription { Id = 4, UserId = "u1", IsActive = true, EndDate = DateTime.UtcNow.AddDays(-1) };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var count = await _service.RefreshExpiredSubscriptionsAsync("u1");
            count.Should().Be(1);
        }
    }
}
