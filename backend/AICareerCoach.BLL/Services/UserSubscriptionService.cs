using AICareerCoach.DAL;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Subscription;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly ILogger<UserSubscriptionService> _logger;

        public UserSubscriptionService(AICareerCoachDbContext context, ILogger<UserSubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GeneralResponse<List<UserSubscription>>> GetAllByUserIdAsync(string userId)
        {
            var list = await _context.UserSubscriptions
                .Include(x => x.Subscription)
                .Include(x => x.Payments)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            return new GeneralResponse<List<UserSubscription>>
            {
                Data = list,
                Success = true,
            };
        }

        public async Task<GeneralResponse<List<UserSubscription>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.UserSubscriptions
                .Include(x => x.User)
                .Include(x => x.Subscription)
                .Include(x => x.Payments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.User != null && x.User.FullName != null && x.User.FullName.ToLower().Contains(term)) ||
                    (x.User != null && x.User.Email != null && x.User.Email.ToLower().Contains(term)));
            }

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreatedAt <= to.Value);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new GeneralResponse<List<UserSubscription>>
            {
                Data = list,
                Success = true,
            };
        }

        public async Task<GeneralResponse<string>> CancelSubscriptionAsync(int id, string userId)
        {
            var sub = await _context.UserSubscriptions
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sub == null)
                return new GeneralResponse<string> { Success = false, Data = "subscription not found" };

            if (sub.UserId != userId)
                return new GeneralResponse<string> { Success = false, Data = "unauthorized" };

            if (!sub.IsActive)
                return new GeneralResponse<string> { Success = false, Data = "subscription is already inactive" };

            sub.IsActive = false;
            sub.Status = SubscriptionStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubId} soft-cancelled for user {UserId} (access until {EndDate})",
                id, userId, sub.EndDate);

            return new GeneralResponse<string> { Success = true, Data = "cancelled successfully" };
        }

        public async Task<int> RefreshExpiredSubscriptionsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var expiredSubs = await _context.UserSubscriptions
                .Where(us => us.UserId == userId && us.IsActive && us.EndDate != null && us.EndDate <= now)
                .ToListAsync();

            if (expiredSubs.Count == 0)
                return 0;

            foreach (var sub in expiredSubs)
            {
                sub.IsActive = false;
                sub.Status = SubscriptionStatus.Expired;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refreshed {Count} expired subscription(s) for user {UserId}", expiredSubs.Count, userId);
            return expiredSubs.Count;
        }

        public async Task<SubscriptionGateStatusDto> GetStatusAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var activeSub = await _context.UserSubscriptions
                .Include(x => x.Subscription)
                .Where(x => x.UserId == userId && x.IsActive && x.EndDate > now)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefaultAsync();

            var hasActive = activeSub != null;
            var planLimits = hasActive && activeSub?.Subscription != null
                ? PlanLimits.FromJson(activeSub.Subscription.LimitsJson)
                : null;

            var interviewUsed = await _context.InterviewSessions.CountAsync(s => s.UserId == userId);
            var roadmapUsed = await _context.UserRoadmaps.CountAsync(r => r.UserId == userId);

            var interviewLimit = hasActive && planLimits != null
                ? planLimits.InterviewSessions
                : FreeLimits.InterviewSessions;
            var roadmapLimit = hasActive && planLimits != null
                ? planLimits.RoadmapGenerations
                : FreeLimits.RoadmapGenerations;
            var jobsLimit = hasActive && planLimits != null
                ? planLimits.JobRecommendations
                : FreeLimits.JobRecommendations;

            return new SubscriptionGateStatusDto
            {
                HasActiveSub = hasActive,
                PlanName = activeSub?.Subscription?.Name,
                EndDate = activeSub?.EndDate,
                Features = new GateFeaturesDto
                {
                    Interview = new GateFeatureStatus
                    {
                        Used = interviewUsed,
                        Limit = interviewLimit,
                        Allowed = interviewLimit == -1 || interviewUsed < interviewLimit,
                    },
                    Roadmap = new GateFeatureStatus
                    {
                        Used = roadmapUsed,
                        Limit = roadmapLimit,
                        Allowed = roadmapLimit == -1 || roadmapUsed < roadmapLimit,
                    },
                    Jobs = new GateFeatureStatus
                    {
                        Used = 0,
                        Limit = jobsLimit,
                        Allowed = true,
                    },
                },
            };
        }

        public async Task<GeneralResponse<PagedPaymentHistoryDto>> GetPaymentHistoryAsync(string userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _context.Payments
                .Include(p => p.UserSubscription!)
                    .ThenInclude(us => us.Subscription)
                .Where(p => p.UserSubscription != null && p.UserSubscription.UserId == userId)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = payments.Select(p => new PaymentInvoiceDto
            {
                PaymentId = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                PlanName = p.UserSubscription?.Subscription?.Name ?? "Subscription",
                Amount = p.Amount,
                Currency = "EGP",
                PaidAt = p.UpdatedAt ?? p.CreatedAt,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                Status = p.Status.ToString(),
            }).ToList();

            var result = new PagedPaymentHistoryDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasNextPage = (page * pageSize) < totalCount,
            };

            return new GeneralResponse<PagedPaymentHistoryDto> { Success = true, Data = result };
        }

        public async Task<GeneralResponse<PaymentInvoiceDto>> GetPaymentInvoiceAsync(int paymentId, string userId)
        {
            var payment = await _context.Payments
                .Include(p => p.UserSubscription!)
                    .ThenInclude(us => us.Subscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null || payment.UserSubscription == null)
                return new GeneralResponse<PaymentInvoiceDto> { Success = false, Data = null! };

            if (payment.UserSubscription.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access invoice for payment {PaymentId} owned by {OwnerId}",
                    userId, paymentId, payment.UserSubscription.UserId);
                return new GeneralResponse<PaymentInvoiceDto> { Success = false, Data = null! };
            }

            var invoice = new PaymentInvoiceDto
            {
                PaymentId = payment.Id,
                InvoiceNumber = payment.InvoiceNumber,
                PlanName = payment.UserSubscription.Subscription?.Name ?? "Subscription",
                Amount = payment.Amount,
                Currency = "EGP",
                PaidAt = payment.UpdatedAt ?? payment.CreatedAt,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                Status = payment.Status.ToString(),
            };

            return new GeneralResponse<PaymentInvoiceDto> { Success = true, Data = invoice };
        }

        public async Task<GeneralResponse<SubscriberDetailDto>> GetSubscriberDetailAsync(int id)
        {
            var sub = await _context.UserSubscriptions
                .Include(x => x.User)
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sub == null)
            {
                _logger.LogWarning("GetSubscriberDetailAsync: subscription {SubId} not found", id);
                return new GeneralResponse<SubscriberDetailDto> { Success = false, Data = null! };
            }

            var payments = await _context.Payments
                .Include(p => p.UserSubscription!)
                    .ThenInclude(us => us!.Subscription)
                .Where(p => p.UserSubscriptionId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new PaymentInvoiceDto
                {
                    PaymentId = p.Id,
                    InvoiceNumber = p.InvoiceNumber,
                    PlanName = p.UserSubscription!.Subscription!.Name ?? "Subscription",
                    Amount = p.Amount,
                    Currency = "EGP",
                    PaidAt = p.UpdatedAt ?? p.CreatedAt,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    Status = p.Status.ToString(),
                })
                .ToListAsync();

            int? daysRemaining = null;
            if (sub.EndDate.HasValue)
            {
                var diff = (sub.EndDate.Value - DateTime.UtcNow).Days;
                daysRemaining = diff > 0 ? diff : 0;
            }

            var cvCount = sub.UserId != null
                ? await _context.CVs.CountAsync(c => c.UserId == sub.UserId)
                : 0;

            var detail = new SubscriberDetailDto
            {
                User = new SubscriberUserDetail
                {
                    Id = sub.UserId ?? "",
                    Email = sub.User?.Email ?? "",
                    FullName = sub.User?.FullName ?? "",
                    JoinDate = sub.User?.CreatedAt ?? sub.CreatedAt,
                    CvCount = cvCount,
                },
                Subscription = new SubscriptionDetail
                {
                    Id = sub.Id,
                    PlanName = sub.Subscription?.Name ?? "—",
                    Status = sub.Status.ToString(),
                    IsActive = sub.IsActive,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate,
                    DaysRemaining = daysRemaining,
                    Amount = sub.Subscription?.Price ?? 0m,
                    Currency = "EGP",
                },
                RecentPayments = payments,
                AuditLogEntries = await _context.SubscriptionAuditLogs
                    .Where(al => al.UserSubscriptionId == id)
                    .Include(al => al.AdminUser)
                    .OrderByDescending(al => al.CreatedAt)
                    .Select(al => new AuditLogDto
                    {
                        Id = al.Id,
                        AdminUserId = al.AdminUserId ?? "",
                        AdminUserName = al.AdminUser!.FullName ?? al.AdminUser.UserName ?? "",
                        Action = al.Action,
                        UserSubscriptionId = al.UserSubscriptionId,
                        TargetUserId = al.TargetUserId,
                        OldValues = al.PreviousValues,
                        NewValues = al.NewValues,
                        Notes = al.Notes,
                        CreatedAt = al.CreatedAt,
                    })
                    .ToListAsync(),
            };

            return new GeneralResponse<SubscriberDetailDto> { Success = true, Data = detail };
        }

        public async Task<RevenueAnalyticsDto> GetAnalyticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            var from = fromDate ?? DateTime.MinValue;
            var to = toDate ?? now;

            var paidPayments = _context.Payments.Where(p => p.Status == PaymentStatus.Paid);
            var paidInRange = paidPayments.Where(p => p.UpdatedAt >= from && p.UpdatedAt <= to);

            var totalRevenue = await paidPayments.SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var revenueInRange = await paidInRange.SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var mrr = await _context.UserSubscriptions
                .Where(us => us.IsActive && us.EndDate > now && us.Subscription != null)
                .SumAsync(us => (decimal?)(us.Subscription!.Price /
                    (us.Subscription!.DurationMonths == 0 ? 1 : us.Subscription!.DurationMonths))) ?? 0m;

            var subscriberStats = await _context.UserSubscriptions
                .GroupBy(us => us.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalSubscribers = subscriberStats.Sum(s => s.Count);
            var activeSubscribers = subscriberStats.FirstOrDefault(s => s.Status == SubscriptionStatus.Active)?.Count ?? 0;
            var pendingSubscribers = subscriberStats.FirstOrDefault(s => s.Status == SubscriptionStatus.Pending)?.Count ?? 0;
            var cancelledSubscribers = subscriberStats.FirstOrDefault(s => s.Status == SubscriptionStatus.Cancelled)?.Count ?? 0;
            var expiredSubscribers = subscriberStats.FirstOrDefault(s => s.Status == SubscriptionStatus.Expired)?.Count ?? 0;

            var arpu = totalSubscribers > 0 ? totalRevenue / totalSubscribers : 0m;

            var thirtyDaysAgo = now.AddDays(-30);
            var cancellationsInLast30d = await _context.UserSubscriptions
                .CountAsync(us => us.Status == SubscriptionStatus.Cancelled && us.UpdatedAt >= thirtyDaysAgo);
            var activeAtPeriodStart = await _context.UserSubscriptions
                .CountAsync(us => us.CreatedAt < thirtyDaysAgo &&
                    (us.IsActive || (us.EndDate != null && us.EndDate > thirtyDaysAgo)));
            var churnRate = activeAtPeriodStart > 0
                ? ((decimal)cancellationsInLast30d / activeAtPeriodStart) * 100
                : 0m;

            var sixMonthsAgo = now.AddMonths(-6);
            var paymentsByMonth = await paidPayments
                .Where(p => p.UpdatedAt >= sixMonthsAgo)
                .GroupBy(p => new { p.UpdatedAt!.Value.Year, p.UpdatedAt!.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            var revenueByMonth = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var d = now.AddMonths(-i);
                    var match = paymentsByMonth.FirstOrDefault(p => p.Year == d.Year && p.Month == d.Month);
                    return new MonthlyRevenuePoint
                    {
                        Month = $"{d.Year:D4}-{d.Month:D2}",
                        MonthLabel = d.ToString("MMM yyyy"),
                        Revenue = match?.Revenue ?? 0m,
                        TransactionCount = match?.Count ?? 0
                    };
                })
                .OrderBy(p => p.Month)
                .ToList();

            var planBreakdown = await _context.UserSubscriptions
                .Where(us => us.Subscription != null)
                .GroupBy(us => new { us.SubscriptionId, us.Subscription!.Name })
                .Select(g => new
                {
                    PlanId = g.Key.SubscriptionId ?? 0,
                    PlanName = g.Key.Name ?? "Unknown",
                    SubscriberCount = g.Count(),
                    ActiveCount = g.Count(us => us.IsActive),
                })
                .ToListAsync();

            var revenueByPlanDict = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid && p.UserSubscription != null)
                .GroupBy(p => p.UserSubscription!.SubscriptionId)
                .Select(g => new { PlanId = g.Key, Revenue = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.PlanId ?? 0, x => x.Revenue);

            var planColors = new[] { "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6" };
            var subscriptionsByPlan = planBreakdown
                .OrderByDescending(p => p.SubscriberCount)
                .Select((p, i) => new PlanBreakdown
                {
                    PlanId = p.PlanId,
                    PlanName = p.PlanName,
                    SubscriberCount = p.SubscriberCount,
                    ActiveCount = p.ActiveCount,
                    Revenue = revenueByPlanDict.GetValueOrDefault(p.PlanId, 0m),
                    Color = planColors[i % planColors.Length]
                })
                .ToList();

            var recentTransactions = await _context.Payments
                .Include(p => p.UserSubscription!)
                    .ThenInclude(us => us!.User)
                .Include(p => p.UserSubscription!)
                    .ThenInclude(us => us!.Subscription)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new RecentTransaction
                {
                    PaymentId = p.Id,
                    UserName = p.UserSubscription!.User!.FullName ?? "",
                    UserEmail = p.UserSubscription.User.Email ?? "",
                    PlanName = p.UserSubscription.Subscription!.Name ?? "",
                    Amount = p.Amount,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation(
                "Analytics computed: range={From}..{To}, revenue={Revenue}, active={Active}, churn={Churn}%",
                from, to, totalRevenue, activeSubscribers, churnRate);

            return new RevenueAnalyticsDto
            {
                Summary = new RevenueSummary
                {
                    Currency = "EGP",
                    TotalRevenue = totalRevenue,
                    MonthlyRecurringRevenue = mrr,
                    AverageRevenuePerUser = arpu,
                    ChurnRate = Math.Round(churnRate, 2),
                    TotalSubscribers = totalSubscribers,
                    ActiveSubscribers = activeSubscribers,
                    PendingSubscribers = pendingSubscribers,
                    CancelledSubscribers = cancelledSubscribers,
                    ExpiredSubscribers = expiredSubscribers,
                },
                RevenueByMonth = revenueByMonth,
                SubscriptionsByPlan = subscriptionsByPlan,
                RecentTransactions = recentTransactions,
            };
        }
    }
}
