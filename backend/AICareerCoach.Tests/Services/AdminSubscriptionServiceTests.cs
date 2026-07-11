using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AICareerCoach.Tests.Services
{
    public class AdminSubscriptionServiceTests
    {
        private readonly AICareerCoachDbContext _context;
        private readonly AdminSubscriptionService _service;

        public AdminSubscriptionServiceTests()
        {
            _context = TestHelpers.CreateInMemoryContext();
            var logger = new Mock<ILogger<AdminSubscriptionService>>();
            _service = new AdminSubscriptionService(_context, logger.Object);
        }

        [Fact]
        public async Task GetSubscriberDetailAsync_WhenNotFound_ReturnsFailure()
        {
            var res = await _service.GetSubscriberDetailAsync(999);
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetSubscriberDetailAsync_WhenExists_ReturnsDetail()
        {
            var sub = new UserSubscription { Id = 1, UserId = "u1", SubscriptionId = 1 };
            _context.UserSubscriptions.Add(sub);
            _context.Subscriptions.Add(new Subscription { Id = 1, Name = "P" });
            _context.SaveChanges();

            var res = await _service.GetSubscriberDetailAsync(1);
            res.Success.Should().BeTrue();
            res.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task ActivateSubscriptionAsync_WhenNotFound_ReturnsFail()
        {
            var res = await _service.ActivateSubscriptionAsync(999, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ActivateSubscriptionAsync_WhenAlreadyActive_ReturnsFail()
        {
            var us = new UserSubscription { Id = 2, IsActive = true, Status = SubscriptionStatus.Active };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.ActivateSubscriptionAsync(2, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ActivateSubscriptionAsync_WhenValid_Activates()
        {
            var sub = new Subscription { Id = 5, DurationMonths = 1 };
            var us = new UserSubscription { Id = 3, SubscriptionId = 5, IsActive = false, Status = SubscriptionStatus.Pending };
            _context.Subscriptions.Add(sub);
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.ActivateSubscriptionAsync(3, "ok", "admin");
            res.Success.Should().BeTrue();

            var saved = await _context.UserSubscriptions.FindAsync(3);
            saved!.IsActive.Should().BeTrue();
            saved.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenNotFound_ReturnsFail()
        {
            var res = await _service.CancelSubscriptionAsync(999, "n", false, "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenInactive_ReturnsFail()
        {
            var us = new UserSubscription { Id = 4, IsActive = false };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.CancelSubscriptionAsync(4, "n", false, "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CancelSubscriptionAsync_WhenValid_Cancels()
        {
            var us = new UserSubscription { Id = 6, IsActive = true, Status = SubscriptionStatus.Active, EndDate = DateTime.UtcNow.AddDays(10) };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.CancelSubscriptionAsync(6, "n", true, "admin");
            res.Success.Should().BeTrue();

            var saved = await _context.UserSubscriptions.FindAsync(6);
            saved!.IsActive.Should().BeFalse();
            saved.Status.Should().Be(SubscriptionStatus.Cancelled);
        }

        [Fact]
        public async Task ExtendSubscriptionAsync_WhenInvalidDays_ReturnsFail()
        {
            var res = await _service.ExtendSubscriptionAsync(1, 0, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ExtendSubscriptionAsync_WhenNotFound_ReturnsFail()
        {
            var res = await _service.ExtendSubscriptionAsync(999, 5, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ExtendSubscriptionAsync_WhenValid_Extends()
        {
            var us = new UserSubscription { Id = 7, IsActive = false };
            _context.UserSubscriptions.Add(us);
            _context.SaveChanges();

            var res = await _service.ExtendSubscriptionAsync(7, 3, "n", "admin");
            res.Success.Should().BeTrue();

            var saved = await _context.UserSubscriptions.FindAsync(7);
            saved!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task MarkPaymentPaidAsync_WhenNotFound_ReturnsFail()
        {
            var res = await _service.MarkPaymentPaidAsync(999, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task MarkPaymentPaidAsync_WhenAlreadyPaid_ReturnsFail()
        {
            var p = new Payment { Id = 8, Status = PaymentStatus.Paid };
            _context.Payments.Add(p);
            _context.SaveChanges();

            var res = await _service.MarkPaymentPaidAsync(8, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task MarkPaymentPaidAsync_WhenValid_MarksPaid()
        {
            var s = new Subscription { Id = 10, DurationMonths = 1 };
            var us = new UserSubscription { Id = 11, SubscriptionId = 10, IsActive = false };
            var p = new Payment { Id = 12, Status = PaymentStatus.Pending, UserSubscriptionId = 11 };
            _context.Subscriptions.Add(s);
            _context.UserSubscriptions.Add(us);
            _context.Payments.Add(p);
            _context.SaveChanges();

            var res = await _service.MarkPaymentPaidAsync(12, "n", "admin");
            res.Success.Should().BeTrue();

            var saved = await _context.Payments.FindAsync(12);
            saved!.Status.Should().Be(PaymentStatus.Paid);
        }

        [Fact]
        public async Task RefundPaymentAsync_WhenNotFound_ReturnsFail()
        {
            var res = await _service.RefundPaymentAsync(999, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task RefundPaymentAsync_WhenNotPaid_ReturnsFail()
        {
            var p = new Payment { Id = 20, Status = PaymentStatus.Pending };
            _context.Payments.Add(p);
            _context.SaveChanges();

            var res = await _service.RefundPaymentAsync(20, "n", "admin");
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task RefundPaymentAsync_WhenPaid_CreatesRefund()
        {
            var p = new Payment { Id = 21, Status = PaymentStatus.Paid, Amount = 50, InvoiceNumber = "INV-1" };
            _context.Payments.Add(p);
            _context.SaveChanges();

            var res = await _service.RefundPaymentAsync(21, "n", "admin");
            res.Success.Should().BeTrue();

            var refunds = await _context.Payments.ToListAsync();
            refunds.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task GetAuditLogAsync_WhenNoSubscription_ReturnsFalse()
        {
            var res = await _service.GetAuditLogAsync(999);
            res.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetAuditLogAsync_WhenExists_ReturnsLogs()
        {
            _context.UserSubscriptions.Add(new UserSubscription { Id = 30 });
            _context.SubscriptionAuditLogs.Add(new SubscriptionAuditLog { Id = 1, UserSubscriptionId = 30, Action = "X", AdminUserId = "a" });
            _context.SaveChanges();

            var res = await _service.GetAuditLogAsync(30);
            res.Success.Should().BeTrue();
            res.Data.Should().HaveCount(1);
        }
    }
}
