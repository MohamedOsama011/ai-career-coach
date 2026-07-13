using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Subscription;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminService> _logger;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public AdminService(
            AICareerCoachDbContext context,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminService> logger,
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            var usersCount = await _context.Users.CountAsync();
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var cvsCount = await _context.CVs.CountAsync();
            var interviewsCount = await _context.InterviewSessions.CountAsync();
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var activeSubs = await _context.UserSubscriptions
                .CountAsync(s => s.IsActive && s.EndDate > DateTime.UtcNow);

            return new DashboardStatisticsDto
            {
                Users = usersCount,
                Admins = admins.Count,
                CVs = cvsCount,
                Interviews = interviewsCount,
                TotalRevenue = totalRevenue,
                ActiveSubscriptions = activeSubs
            };
        }

        public async Task<List<AdminUserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var adminUsers = new HashSet<string>(
                (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id));

            return users.Select(user => new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                CareerGoal = user.CareerGoal,
                Role = adminUsers.Contains(user.Id) ? "Admin" : "User"
            }).ToList();
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == id)
                return false;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return false;

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                    return false;
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogWarning("Admin {AdminId} deleted user {UserId} ({Email})", currentUserId, id, user.Email);
                await LogAuditAsync(currentUserId, "delete_user", "User", id,
                    $"Deleted user: {user.FullName} ({user.Email})");
            }
            return result.Succeeded;
        }

        public async Task<bool> ChangeUserRoleAsync(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return false;

            var roles = await _userManager.GetRolesAsync(user);
            var oldRole = roles.FirstOrDefault() ?? "None";
            if (roles.Contains("Admin") && role != "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                    return false;
            }

            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);

            var result = await _userManager.AddToRoleAsync(user, role);
            if (result.Succeeded)
            {
                var adminId = GetCurrentUserId();
                _logger.LogInformation("Admin {AdminId} changed role of user {UserId} from {OldRole} to {NewRole}",
                    adminId, id, oldRole, role);
                await LogAuditAsync(adminId, "change_role", "User", id,
                    $"Changed role from {oldRole} to {role}");
            }
            return result.Succeeded;
        }

        public async Task<List<CVAdminDto>> GetAllCVsAsync()
        {
            var cvs = await _context.CVs
                .Include(c => c.User)
                .ToListAsync();

            return cvs.Select(c =>
            {
                var fileName = Path.GetFileName(c.FilePath);
                var index = fileName.IndexOf('_');
                if (index > 0)
                    fileName = fileName.Substring(index + 1);

                return new CVAdminDto
                {
                    Id = c.CVId,
                    UserName = c.User!.FullName,
                    UserEmail = c.User.Email ?? "",
                    FileName = fileName,
                    UploadDate = c.UploadedAt
                };
            }).ToList();
        }

        public async Task<bool> DeleteCVAsync(int id)
        {
            var cv = await _context.CVs.FirstOrDefaultAsync(x => x.CVId == id);
            if (cv == null)
                return false;

            _context.CVs.Remove(cv);
            await _context.SaveChangesAsync();

            var adminId = GetCurrentUserId();
            _logger.LogWarning("Admin {AdminId} deleted CV {CvId} for user {UserId}", adminId, id, cv.UserId);
            await LogAuditAsync(adminId, "delete_cv", "CV", id.ToString(),
                $"Deleted CV (file: {cv.FilePath}) for user {cv.UserId}");

            return true;
        }

        public async Task<DownloadCVDto?> DownloadCVAsync(int id)
        {
            var cv = await _context.CVs.FindAsync(id);
            if (cv == null)
                return null;

            return new DownloadCVDto
            {
                FilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "cvs",
                    cv.FilePath),
                FileName = Path.GetFileName(cv.FilePath)
            };
        }

        public async Task<List<SyncLogDto>> GetSyncLogsAsync(int count = 50)
        {
            var logs = await _context.JobSyncLogs
                .OrderByDescending(l => l.SyncedAt)
                .Take(count)
                .Select(l => new SyncLogDto
                {
                    Id = l.Id,
                    SyncedAt = l.SyncedAt,
                    Status = l.Errors > 0
                        ? (l.Fetched > 0 ? "Warning" : "Failed")
                        : "Success",
                    FetchedCount = l.Fetched,
                    NewCount = l.New,
                    SkippedCount = l.Skipped,
                    EmbeddedCount = l.Embedded,
                    ErrorCount = l.Errors,
                    ErrorMessages = l.ErrorMessages,
                    DurationMs = (long)l.Duration.TotalMilliseconds,
                })
                .ToListAsync();
            return logs;
        }

        public async Task<List<UserManagementDto>> GetUserManagementAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var adminUsers = new HashSet<string>(
                (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id));

            var result = new List<UserManagementDto>();

            foreach (var user in users)
            {
                var role = adminUsers.Contains(user.Id) ? "Admin" : "User";

                result.Add(new UserManagementDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Role = role,
                    CareerGoal = user.CareerGoal,
                    HasCv = await _context.CVs.AnyAsync(c => c.UserId == user.Id),
                    InterviewsCount = await _context.InterviewSessions.CountAsync(i => i.UserId == user.Id),
                    Plan = "Free",
                    AmountPaid = 0,
                    CreatedAt = user.CreatedAt
                });
            }

            return result;
        }

        public async Task<UserDetailDto?> GetUserDetailAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return null;

            var adminUsers = new HashSet<string>(
                (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id));

            var role = adminUsers.Contains(user.Id) ? "Admin" : "User";

            var cvs = await _context.CVs
                .Where(c => c.UserId == id)
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new SubscriberCvDto
                {
                    CvId = c.CVId,
                    FileName = c.FilePath.Substring(c.FilePath.IndexOf('_') + 1),
                    UploadedAt = c.UploadedAt
                })
                .ToListAsync();

            var interviewCount = await _context.InterviewSessions.CountAsync(i => i.UserId == id);

            var recentSessions = await _context.InterviewSessions
                .Where(i => i.UserId == id)
                .OrderByDescending(i => i.CreatedAt)
                .Take(5)
                .Select(i => new SubscriberSessionDto
                {
                    Id = i.Id,
                    Track = i.Track.ToString(),
                    Difficulty = i.Difficulty.ToString(),
                    TargetRole = i.TargetRole,
                    Status = i.Status.ToString(),
                    QuestionsAsked = i.QuestionsAsked,
                    MaxQuestions = i.MaxQuestions,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            var roadmaps = await _context.UserRoadmaps
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new SubscriberRoadmapDto
                {
                    Id = r.Id,
                    TargetRole = r.TargetRole,
                    TemplateTrack = r.TemplateTrack,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.UserSubscription != null && p.UserSubscription.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new PaymentInvoiceDto
                {
                    PaymentId = p.Id,
                    InvoiceNumber = p.InvoiceNumber,
                    PlanName = p.UserSubscription!.Subscription!.Name,
                    Amount = p.Amount,
                    Currency = "EGP",
                    PaidAt = p.CreatedAt,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    Status = p.Status.ToString()
                })
                .ToListAsync();

            return new UserDetailDto
            {
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Phone = user.PhoneNumber,
                    Role = role,
                    CareerGoal = user.CareerGoal,
                    CreatedAt = user.CreatedAt
                },
                CVs = cvs,
                Interviews = new UserInterviewInfo
                {
                    TotalCount = interviewCount,
                    RecentSessions = recentSessions
                },
                Roadmaps = roadmaps,
                Payments = payments
            };
        }

        public async Task ClearCacheAsync(int? userId)
        {
            var adminId = GetCurrentUserId();

            if (userId.HasValue)
            {
                await _context.AiFeedbackCaches
                    .Where(c => c.UserId == userId.Value.ToString())
                    .ExecuteDeleteAsync();

                await _context.JobRecommendationCaches
                    .Where(c => c.UserId == userId.Value.ToString())
                    .ExecuteDeleteAsync();

                _logger.LogWarning("Admin {AdminId} cleared AI cache for user {UserId}", adminId, userId);

                await LogAuditAsync(adminId, "clear_cache", "User", userId.Value.ToString(),
                    "Cleared AI feedback cache + job recommendation cache for user");
            }
            else
            {
                await _context.AiFeedbackCaches
                    .ExecuteDeleteAsync();

                await _context.JobRecommendationCaches
                    .ExecuteDeleteAsync();

                _logger.LogWarning("Admin {AdminId} cleared ALL AI cache", adminId);

                await LogAuditAsync(adminId, "clear_cache", "System", null,
                    "Cleared ALL AI feedback cache + job recommendation cache");
            }
        }

        public async Task LogAuditAsync(string adminUserId, string action, string targetType, string? targetId, string? details)
        {
            try
            {
                var log = new AdminAuditLog
                {
                    AdminUserId = string.IsNullOrWhiteSpace(adminUserId) ? null : adminUserId,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.AdminAuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log (admin={AdminId}, action={Action})", adminUserId, action);
            }
        }

        public async Task<PaginatedAuditLogsDto> GetAuditLogsAsync(int page, int pageSize, string? action, string? adminId)
        {
            try
            {
                var query = _context.AdminAuditLogs
                    .Include(l => l.AdminUser)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(action))
                    query = query.Where(l => l.Action == action);
                if (!string.IsNullOrWhiteSpace(adminId))
                    query = query.Where(l => l.AdminUserId == adminId);

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new AdminAuditLogDto
                    {
                        Id = l.Id,
                        AdminUserId = l.AdminUserId,
                        AdminUserName = l.AdminUser != null ? l.AdminUser.FullName : "Unknown",
                        Action = l.Action,
                        TargetType = l.TargetType,
                        TargetId = l.TargetId,
                        Details = l.Details,
                        Timestamp = l.Timestamp
                    })
                    .ToListAsync();

                return new PaginatedAuditLogsDto
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load audit logs, returning empty result");
                return new PaginatedAuditLogsDto
                {
                    Items = new List<AdminAuditLogDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }
        }

        public async Task<HealthCheckDto> GetHealthAsync()
        {
            var result = new HealthCheckDto
            {
                Version = typeof(AdminService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                Uptime = FormatUptime(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime())
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                sw.Stop();
                result.Db = new HealthComponentStatus
                {
                    Status = canConnect ? "healthy" : "unhealthy",
                    Message = canConnect ? "Connected" : "Cannot connect to database",
                    LatencyMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.Db = new HealthComponentStatus
                {
                    Status = "unhealthy",
                    Message = ex.Message,
                    LatencyMs = sw.ElapsedMilliseconds
                };
            }

            var gitHubToken = _configuration["GitHub:Token"];
            result.Llm = new HealthComponentStatus
            {
                Status = !string.IsNullOrEmpty(gitHubToken) ? "healthy" : "unhealthy",
                Message = !string.IsNullOrEmpty(gitHubToken) ? "API key configured" : "GitHub:Token not configured"
            };

            var joobleKey = _configuration["Jooble:ApiKey"];
            var logoDevKey = _configuration["LogoDev:ApiKey"];
            result.JobProvider = new HealthComponentStatus
            {
                Status = !string.IsNullOrEmpty(joobleKey) ? "healthy" : "unhealthy",
                Message = !string.IsNullOrEmpty(joobleKey) ? "Jooble API key configured" : "Jooble:ApiKey not configured"
            };

            try
            {
                var lastSync = await _context.JobSyncLogs
                    .OrderByDescending(l => l.SyncedAt)
                    .FirstOrDefaultAsync();

                if (lastSync != null)
                {
                    result.LastSyncTime = lastSync.SyncedAt;
                    result.LastSyncSuccess = lastSync.Errors == 0;
                }
            }
            catch
            {
            }

            try
            {
                var cvsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cvs");
                if (Directory.Exists(cvsDir))
                {
                    var dirInfo = new DirectoryInfo(cvsDir);
                    var usedBytes = dirInfo.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                    var drive = DriveInfo.GetDrives()
                        .FirstOrDefault(d => cvsDir.StartsWith(d.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase));

                    if (drive != null && drive.IsReady)
                    {
                        var usedPercent = (double)usedBytes / drive.TotalSize * 100;
                        result.Storage = new StorageHealthStatus
                        {
                            Status = usedPercent < 80 ? "healthy" : usedPercent < 95 ? "warning" : "unhealthy",
                            Message = usedPercent < 80 ? "Sufficient space" : usedPercent < 95 ? "Running low on disk space" : "Critical — almost full",
                            UsedPercent = Math.Round(usedPercent, 1),
                            UsedBytes = usedBytes,
                            TotalBytes = drive.TotalSize
                        };
                    }
                    else
                    {
                        result.Storage = new StorageHealthStatus
                        {
                            Status = "healthy",
                            Message = "Storage check unavailable — drive info not accessible",
                            UsedBytes = usedBytes
                        };
                    }
                }
                else
                {
                    result.Storage = new StorageHealthStatus
                    {
                        Status = "warning",
                        Message = "CV upload directory does not exist yet"
                    };
                }
            }
            catch (Exception ex)
            {
                result.Storage = new StorageHealthStatus
                {
                    Status = "unhealthy",
                    Message = ex.Message
                };
            }

            return result;
        }

        public async Task SendBroadcastToAllAsync(string title, string body, string type)
        {
            var adminId = GetCurrentUserId();
            await _notificationService.SendToAllAsync(title, body, type);
            await LogAuditAsync(adminId, "broadcast", "all", null, $"Broadcast: {title} (type: {type})");
        }

        public async Task SendBroadcastToPlanAsync(string planName, string title, string body, string type)
        {
            var adminId = GetCurrentUserId();
            await _notificationService.SendToPlanAsync(planName, title, body, type);
            await LogAuditAsync(adminId, "broadcast", "plan", planName, $"Broadcast to {planName}: {title} (type: {type})");
        }

        public async Task SendBroadcastToUserAsync(string userId, string title, string body, string type)
        {
            var adminId = GetCurrentUserId();
            await _notificationService.SendToUserAsync(userId, title, body, type);
            await LogAuditAsync(adminId, "broadcast", "user", userId, $"Broadcast to user {userId}: {title} (type: {type})");
        }

        public async Task<ReportsDto> GetReportsAsync()
        {
            var now = DateTime.UtcNow;

            var usersOverTime = new List<MonthlyPoint>();
            for (var i = 11; i >= 0; i--)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var count = await _context.Users
                    .CountAsync(u => u.CreatedAt.Year == month.Year && u.CreatedAt.Month == month.Month);
                usersOverTime.Add(new MonthlyPoint
                {
                    Month = month.ToString("MMM yyyy"),
                    Count = count
                });
            }

            var interviewsPerDay = new List<DailyPoint>();
            for (var i = 29; i >= 0; i--)
            {
                var date = now.Date.AddDays(-i);
                var count = await _context.InterviewSessions
                    .CountAsync(s => s.CreatedAt.Year == date.Year && s.CreatedAt.Month == date.Month && s.CreatedAt.Day == date.Day);
                interviewsPerDay.Add(new DailyPoint
                {
                    Date = date.ToString("MMM d"),
                    Count = count
                });
            }

            var roleQuery1 = _context.UserRoadmaps
                .Where(r => !string.IsNullOrEmpty(r.TargetRole))
                .GroupBy(r => r.TargetRole)
                .Select(g => new { Role = g.Key, Count = g.Count() });

            var roleQuery2 = _context.InterviewSessions
                .Where(s => !string.IsNullOrEmpty(s.TargetRole))
                .GroupBy(s => s.TargetRole)
                .Select(g => new { Role = g.Key, Count = g.Count() });

            var roles1 = await roleQuery1.ToListAsync();
            var roles2 = await roleQuery2.ToListAsync();

            var mergedRoles = roles1.Concat(roles2)
                .GroupBy(r => r.Role)
                .Select(g => new SimpleCount { Label = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(r => r.Count)
                .Take(10)
                .ToList();

            var allJobs = await _context.Jobs
                .Where(j => !string.IsNullOrEmpty(j.RequiredSkills))
                .Select(j => j.RequiredSkills)
                .ToListAsync();

            var skillCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var skillsJson in allJobs)
            {
                try
                {
                    var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(skillsJson);
                    if (skills != null)
                    {
                        foreach (var skill in skills)
                        {
                            var trimmed = skill.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                skillCounts.TryGetValue(trimmed, out var existing);
                                skillCounts[trimmed] = existing + 1;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            var popularSkills = skillCounts
                .OrderByDescending(kv => kv.Value)
                .Take(15)
                .Select(kv => new SimpleCount { Label = kv.Key, Count = kv.Value })
                .ToList();

            return new ReportsDto
            {
                UsersOverTime = usersOverTime,
                InterviewsPerDay = interviewsPerDay,
                TopRequestedRoles = mergedRoles,
                PopularSkills = popularSkills
            };
        }

        public async Task<byte[]> ExportCsvAsync(string reportType)
        {
            using var sw = new System.IO.StringWriter();

            switch (reportType.ToLowerInvariant())
            {
                case "users":
                    await sw.WriteLineAsync("ID,FullName,Email,Role,CreatedAt,CVCount,InterviewCount");
                    var users = await _userManager.Users.ToListAsync();
                    var adminUsers = new HashSet<string>(
                        (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id));
                    foreach (var u in users)
                    {
                        var cvCount = await _context.CVs.CountAsync(c => c.UserId == u.Id);
                        var interviewCount = await _context.InterviewSessions.CountAsync(i => i.UserId == u.Id);
                        await sw.WriteLineAsync(
                            $"\"{u.Id}\",\"{EscapeCsv(u.FullName)}\",\"{EscapeCsv(u.Email ?? "")}\",\"{(adminUsers.Contains(u.Id) ? "Admin" : "User")}\",\"{u.CreatedAt:yyyy-MM-dd}\",{cvCount},{interviewCount}");
                    }
                    break;

                case "interviews":
                    await sw.WriteLineAsync("ID,UserName,UserEmail,Track,Difficulty,TargetRole,Status,QuestionsAsked,MaxQuestions,CreatedAt,DurationMinutes");
                    var sessions = await _context.InterviewSessions
                        .Include(s => s.User)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync();
                    foreach (var s in sessions)
                    {
                        var duration = s.CompletedAt.HasValue
                            ? (s.CompletedAt.Value - s.CreatedAt).TotalMinutes.ToString("F1")
                            : "";
                        await sw.WriteLineAsync(
                            $"{s.Id},\"{EscapeCsv(s.User?.FullName ?? "")}\",\"{EscapeCsv(s.User?.Email ?? "")}\",\"{s.Track}\",\"{s.Difficulty}\",\"{EscapeCsv(s.TargetRole ?? "")}\",\"{s.Status}\",{s.QuestionsAsked},{s.MaxQuestions},\"{s.CreatedAt:yyyy-MM-dd HH:mm}\",{duration}");
                    }
                    break;

                case "payments":
                    await sw.WriteLineAsync("ID,UserName,UserEmail,PlanName,Amount,Currency,PaymentMethod,TransactionId,Status,CreatedAt");
                    var payments = await _context.Payments
                        .Include(p => p.UserSubscription).ThenInclude(us => us!.User)
                        .Include(p => p.UserSubscription).ThenInclude(us => us!.Subscription)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                    foreach (var p in payments)
                    {
                        await sw.WriteLineAsync(
                            $"{p.Id},\"{EscapeCsv(p.UserSubscription?.User?.FullName ?? "")}\",\"{EscapeCsv(p.UserSubscription?.User?.Email ?? "")}\",\"{EscapeCsv(p.UserSubscription?.Subscription?.Name ?? "")}\",{p.Amount},EGP,\"{EscapeCsv(p.PaymentMethod ?? "")}\",\"{EscapeCsv(p.TransactionId ?? "")}\",\"{p.Status}\",\"{p.CreatedAt:yyyy-MM-dd HH:mm}\"");
                    }
                    break;

                case "cvs":
                    await sw.WriteLineAsync("ID,UserName,UserEmail,FileName,UploadDate");
                    var cvs = await _context.CVs
                        .Include(c => c.User)
                        .OrderByDescending(c => c.UploadedAt)
                        .ToListAsync();
                    foreach (var c in cvs)
                    {
                        var fileName = System.IO.Path.GetFileName(c.FilePath);
                        var idx = fileName.IndexOf('_');
                        if (idx > 0) fileName = fileName[(idx + 1)..];
                        await sw.WriteLineAsync(
                            $"{c.CVId},\"{EscapeCsv(c.User?.FullName ?? "")}\",\"{EscapeCsv(c.User?.Email ?? "")}\",\"{EscapeCsv(fileName)}\",\"{c.UploadedAt:yyyy-MM-dd}\"");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown report type: {reportType}");
            }

            return System.Text.Encoding.UTF8.GetBytes(sw.ToString());
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private static string FormatUptime(TimeSpan uptime)
        {
            var parts = new List<string>();
            if (uptime.Days > 0) parts.Add($"{uptime.Days}d");
            if (uptime.Hours > 0) parts.Add($"{uptime.Hours}h");
            if (uptime.Minutes > 0) parts.Add($"{uptime.Minutes}m");
            if (parts.Count == 0) return "< 1m";
            return string.Join(" ", parts);
        }

        public async Task<PaginatedChatSessionsDto> GetChatSessionsAsync(int page, int pageSize)
        {
            var query = _context.ChatSessions
                .Include(s => s.User)
                .Include(s => s.Messages)
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new ChatSessionAdminDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User.FullName,
                    UserEmail = s.User.Email ?? "",
                    Title = s.Title,
                    MessageCount = s.Messages.Count,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                })
                .ToListAsync();

            return new PaginatedChatSessionsDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            };
        }

        public async Task<List<ChatMessageAdminDto>> GetChatMessagesAsync(int sessionId)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ChatMessageAdminDto
                {
                    Id = m.Id,
                    Role = m.Role.ToString(),
                    Content = m.Content,
                    ToolName = m.ToolName,
                    OrderIndex = m.OrderIndex,
                    CreatedAt = m.CreatedAt,
                })
                .ToListAsync();
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                .Value ?? string.Empty;
        }
    }
}
