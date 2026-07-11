using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class SubscriptionServiceTests
    {
        private readonly AICareerCoachDbContext _context;
        private readonly SubscriptionService _service;

        public SubscriptionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AICareerCoachDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;

            _context = new AICareerCoachDbContext(options);
            _service = new SubscriptionService(_context);
        }

        [Fact]
        public async Task GetAllSubscriptionsAsync_WhenEmpty_ReturnsFalse()
        {
            var res = await _service.GetAllSubscriptionsAsync();
            res.Success.Should().BeFalse();
            res.Data.Should().BeNull();
        }

        [Fact]
        public async Task CreateSubscriptionAsync_AddsSubscription()
        {
            var dto = new SubscriptionDto { Name = "P", Price = 9.9m, DurationMonths = 1 };
            await _service.CreateSubscriptionAsync(dto);

            var all = await _service.GetAllSubscriptionsAsync();
            all.Success.Should().BeTrue();
            all.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteSubscriptionAsync_WhenNotFound_ReturnsNotFound()
        {
            var res = await _service.DeleteSubscriptionAsync("999");
            res.Success.Should().BeFalse();
            res.Data.Should().Be("subscription not found");
        }

        [Fact]
        public async Task DeleteSubscriptionAsync_WhenHasSubscribers_ReturnsCannotDelete()
        {
            var sub = new Subscription { Id = 1, Name = "P" };
            _context.Subscriptions.Add(sub);
            _context.UserSubscriptions.Add(new UserSubscription { Id = 2, SubscriptionId = 1, UserId = "u1" });
            _context.SaveChanges();

            var res = await _service.DeleteSubscriptionAsync("1");
            res.Success.Should().BeFalse();
            ((string)res.Data).Should().Contain("cannot delete plan");
        }

        [Fact]
        public async Task UpdateSubscriptionAsync_WhenNotFound_ReturnsNotFound()
        {
            var dto = new SubscriptionDto { Name = "X", Price = 1, DurationMonths = 1 };
            var res = await _service.UpdateSubscriptionAsync(dto, "999");
            res.Success.Should().BeFalse();
            res.Data.Should().Be("subscription not found");
        }
    }
}
