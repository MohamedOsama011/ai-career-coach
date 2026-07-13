using System.Security.Claims;
using System.Text.Json;
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
    public class AdminSubscriptionService : IAdminSubscriptionService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly ILogger<AdminSubscriptionService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public AdminSubscriptionService(AICareerCoachDbContext context, ILogger<AdminSubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GeneralResponse<SubscriberDetailDto>> GetSubscriberDetailAsync(int id)
        {
            var sub = await _context.UserSubscriptions
                .Include(x => x.User)
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sub == null)
            {
                _logger.LogWarning("GetSubscriberDetail: subscription {SubId} not found", id);
                return new GeneralResponse<SubscriberDetailDto> { Success = false, Data = null! };
            }

            var payments = await _context.Payments
                .Where(p => p.UserSubscriptionId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new PaymentInvoiceDto
                {
                    PaymentId = p.Id,
                    InvoiceNumber = p.InvoiceNumber,
                    PlanName = sub.Subscription!.Name ?? "Subscription",
                    Amount = p.Amount,
                    Currency = "EGP",
                    PaidAt = p.UpdatedAt ?? p.CreatedAt,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    Status = p.Status.ToString(),
                })
                .ToListAsync();

            List<AuditLogDto> auditLogs;
            try
            {
                auditLogs = await _context.SubscriptionAuditLogs
                    .Where(al => al.UserSubscriptionId == id)
                    .Include(al => al.AdminUser)
                    .OrderByDescending(al => al.CreatedAt)
                    .Select(al => new AuditLogDto
                    {
                        Id = al.Id,
                        AdminUserId = al.AdminUserId ?? "",
                        AdminUserName = al.AdminUser != null ? (al.AdminUser.FullName ?? al.AdminUser.UserName ?? "") : "System",
                        Action = al.Action,
                        UserSubscriptionId = al.UserSubscriptionId,
                        TargetUserId = al.TargetUserId,
                        OldValues = al.PreviousValues,
                        NewValues = al.NewValues,
                        Notes = al.Notes,
                        CreatedAt = al.CreatedAt,
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load audit logs for subscriber {SubId}, returning empty list", id);
                auditLogs = new List<AuditLogDto>();
            }

            var sessions = await _context.InterviewSessions
                .Where(s => s.UserId == sub.UserId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new SubscriberSessionDto
                {
                    Id = s.Id,
                    Track = s.Track.ToString(),
                    Difficulty = s.Difficulty.ToString(),
                    TargetRole = s.TargetRole,
                    Status = s.Status.ToString(),
                    QuestionsAsked = s.QuestionsAsked,
                    MaxQuestions = s.MaxQuestions,
                    CreatedAt = s.CreatedAt,
                })
                .ToListAsync();

            var cvs = await _context.CVs
                .Where(c => c.UserId == sub.UserId)
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new SubscriberCvDto
                {
                    CvId = c.CVId,
                    FileName = c.FilePath ?? "",
                    UploadedAt = c.UploadedAt,
                })
                .ToListAsync();

            var roadmaps = await _context.UserRoadmaps
                .Where(r => r.UserId == sub.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SubscriberRoadmapDto
                {
                    Id = r.Id,
                    TargetRole = r.TargetRole,
                    TemplateTrack = r.TemplateTrack,
                    CreatedAt = r.CreatedAt,
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
                    Phone = sub.User?.PhoneNumber,
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
                AuditLogEntries = auditLogs,
                RecentSessions = sessions,
                CVs = cvs,
                Roadmaps = roadmaps,
            };

            return new GeneralResponse<SubscriberDetailDto> { Success = true, Data = detail };
        }

        public async Task<GeneralResponse<string>> ActivateSubscriptionAsync(int subscriptionId, string notes, string adminUserId)
        {
            var sub = await _context.UserSubscriptions
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (sub == null)
                return Fail("subscription not found");

            if (sub.IsActive)
                return Fail("subscription is already active");

            var previousJson = JsonSerializer.Serialize(new
            {
                isActive = sub.IsActive,
                status = sub.Status.ToString(),
                startDate = sub.StartDate,
                endDate = sub.EndDate,
            }, _jsonOpts);

            sub.IsActive = true;
            sub.Status = SubscriptionStatus.Active;
            sub.StartDate = DateTime.UtcNow;
            var durationMonths = sub.Subscription?.DurationMonths ?? 1;
            sub.EndDate = DateTime.UtcNow.AddMonths(durationMonths);

            var newJson = JsonSerializer.Serialize(new
            {
                isActive = sub.IsActive,
                status = sub.Status.ToString(),
                startDate = sub.StartDate,
                endDate = sub.EndDate,
            }, _jsonOpts);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                await _context.SaveChangesAsync();
                WriteAuditLog("Activated", sub.Id, notes, previousJson, newJson, adminUserId, null);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            _logger.LogInformation("Admin {AdminId} activated subscription {SubId}", adminUserId, subscriptionId);
            return Ok("subscription activated");
        }

        public async Task<GeneralResponse<string>> CancelSubscriptionAsync(int subscriptionId, string notes, bool immediate, string adminUserId)
        {
            var sub = await _context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (sub == null)
                return Fail("subscription not found");

            if (!sub.IsActive)
                return Fail("subscription is already inactive");

            var previousJson = JsonSerializer.Serialize(new
            {
                isActive = sub.IsActive,
                status = sub.Status.ToString(),
                endDate = sub.EndDate,
            }, _jsonOpts);

            sub.IsActive = false;
            sub.Status = SubscriptionStatus.Cancelled;
            if (immediate)
                sub.EndDate = DateTime.UtcNow;

            var newJson = JsonSerializer.Serialize(new
            {
                isActive = sub.IsActive,
                status = sub.Status.ToString(),
                endDate = sub.EndDate,
            }, _jsonOpts);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                await _context.SaveChangesAsync();
                WriteAuditLog("Cancelled", sub.Id, notes, previousJson, newJson, adminUserId, null);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            _logger.LogInformation("Admin {AdminId} cancelled subscription {SubId} (immediate={Immediate})",
                adminUserId, subscriptionId, immediate);
            return Ok("subscription cancelled");
        }

        public async Task<GeneralResponse<string>> ExtendSubscriptionAsync(int subscriptionId, int additionalDays, string notes, string adminUserId)
        {
            if (additionalDays < 1)
                return Fail("additionalDays must be at least 1");

            var sub = await _context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);

            if (sub == null)
                return Fail("subscription not found");

            var previousJson = JsonSerializer.Serialize(new
            {
                endDate = sub.EndDate,
                isActive = sub.IsActive,
            }, _jsonOpts);

            sub.EndDate = sub.EndDate.HasValue
                ? sub.EndDate.Value.AddDays(additionalDays)
                : DateTime.UtcNow.AddDays(additionalDays);

            if (!sub.IsActive)
            {
                sub.IsActive = true;
                sub.Status = SubscriptionStatus.Active;
            }

            var newJson = JsonSerializer.Serialize(new
            {
                endDate = sub.EndDate,
                isActive = sub.IsActive,
            }, _jsonOpts);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                await _context.SaveChangesAsync();
                WriteAuditLog("Extended", sub.Id, notes, previousJson, newJson, adminUserId, null);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            _logger.LogInformation("Admin {AdminId} extended subscription {SubId} by {Days} days",
                adminUserId, subscriptionId, additionalDays);
            return Ok($"subscription extended by {additionalDays} days");
        }

        public async Task<GeneralResponse<string>> MarkPaymentPaidAsync(int paymentId, string notes, string adminUserId)
        {
            var payment = await _context.Payments
                .Include(p => p.UserSubscription)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return Fail("payment not found");

            if (payment.Status == PaymentStatus.Paid)
                return Fail("payment is already paid");

            var previousJson = JsonSerializer.Serialize(new
            {
                status = payment.Status.ToString(),
            }, _jsonOpts);

            payment.Status = PaymentStatus.Paid;

            if (payment.UserSubscription != null && !payment.UserSubscription.IsActive)
            {
                payment.UserSubscription.IsActive = true;
                payment.UserSubscription.Status = SubscriptionStatus.Active;
                payment.UserSubscription.StartDate ??= DateTime.UtcNow;
                var sub = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.Id == payment.UserSubscription.SubscriptionId);
                var durationMonths = sub?.DurationMonths ?? 1;
                payment.UserSubscription.EndDate = DateTime.UtcNow.AddMonths(durationMonths);
            }

            var newJson = JsonSerializer.Serialize(new
            {
                status = payment.Status.ToString(),
            }, _jsonOpts);

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                await _context.SaveChangesAsync();
                WriteAuditLog("MarkedPaid", payment.UserSubscriptionId, notes, previousJson, newJson, adminUserId, null);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            _logger.LogInformation("Admin {AdminId} marked payment {PaymentId} as paid", adminUserId, paymentId);
            return Ok("payment marked as paid");
        }

        public async Task<GeneralResponse<string>> RefundPaymentAsync(int paymentId, string notes, string adminUserId)
        {
            var originalPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (originalPayment == null)
                return Fail("payment not found");

            if (originalPayment.Status != PaymentStatus.Paid)
                return Fail("only paid payments can be refunded");

            var refund = new Payment
            {
                UserSubscriptionId = originalPayment.UserSubscriptionId,
                Status = PaymentStatus.Paid,
                Amount = -Math.Abs(originalPayment.Amount),
                InvoiceNumber = $"REF-{originalPayment.InvoiceNumber}",
                PaymentMethod = originalPayment.PaymentMethod,
                TransactionId = originalPayment.TransactionId,
            };

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                _context.Payments.Add(refund);
                await _context.SaveChangesAsync();

                var newValues = JsonSerializer.Serialize(new
                {
                    refundAmount = refund.Amount,
                    originalPaymentId = paymentId,
                }, _jsonOpts);

                WriteAuditLog("Refunded", originalPayment.UserSubscriptionId, notes, null, newValues, adminUserId, null);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            });

            _logger.LogInformation("Admin {AdminId} refunded payment {PaymentId} (amount={Amount})",
                adminUserId, paymentId, refund.Amount);
            return Ok($"refund of {Math.Abs(refund.Amount)} EGP created");
        }

        public async Task<GeneralResponse<List<AuditLogDto>>> GetAuditLogAsync(int subscriptionId)
        {
            var exists = await _context.UserSubscriptions.AnyAsync(x => x.Id == subscriptionId);
            if (!exists)
                return new GeneralResponse<List<AuditLogDto>> { Success = false, Data = null! };

            List<AuditLogDto> logs;
            try
            {
                logs = await _context.SubscriptionAuditLogs
                    .Where(al => al.UserSubscriptionId == subscriptionId)
                    .Include(al => al.AdminUser)
                    .OrderByDescending(al => al.CreatedAt)
                    .Select(al => new AuditLogDto
                    {
                        Id = al.Id,
                        AdminUserId = al.AdminUserId ?? "",
                        AdminUserName = al.AdminUser != null ? (al.AdminUser.FullName ?? al.AdminUser.UserName ?? "") : "System",
                        Action = al.Action,
                        UserSubscriptionId = al.UserSubscriptionId,
                        TargetUserId = al.TargetUserId,
                        OldValues = al.PreviousValues,
                        NewValues = al.NewValues,
                        Notes = al.Notes,
                        CreatedAt = al.CreatedAt,
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load audit log for subscription {SubId}, returning empty list", subscriptionId);
                logs = new List<AuditLogDto>();
            }

            return new GeneralResponse<List<AuditLogDto>> { Success = true, Data = logs };
        }

        private void WriteAuditLog(string action, int? userSubscriptionId, string notes, string? previousValues, string? newValues, string adminUserId, string? targetUserId)
        {
            _context.SubscriptionAuditLogs.Add(new SubscriptionAuditLog
            {
                AdminUserId = string.IsNullOrWhiteSpace(adminUserId) ? null : adminUserId,
                Action = action,
                UserSubscriptionId = userSubscriptionId,
                TargetUserId = targetUserId,
                PreviousValues = previousValues,
                NewValues = newValues,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            });
        }

        private static GeneralResponse<string> Fail(string message)
            => new() { Success = false, Data = message };

        private static GeneralResponse<string> Ok(string message)
            => new() { Success = true, Data = message };
    }
}
