using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Admin.AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AICareerCoach.BLL.services
{
    public class AdminService : IAdminService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AdminService(
                AICareerCoachDbContext context,
                UserManager<User> userManager,
                IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            var usersCount = await _context.Users.CountAsync();

            var adminsCount =
                (await _userManager.GetUsersInRoleAsync("Admin")).Count;

            var cvsCount =
                await _context.Set<CV>().CountAsync();

            var interviewsCount =
                await _context.Set<InterviewSession>().CountAsync();

            return new DashboardStatisticsDto
            {
                Users = usersCount,
                Admins = adminsCount,
                CVs = cvsCount,
                Interviews = interviewsCount
            };
        }

        public async Task<List<AdminUserDto>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList();

            var result = new List<AdminUserDto>();

            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user))
                                .FirstOrDefault() ?? "";

                result.Add(new AdminUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    CareerGoal = user.CareerGoal,
                    Role = role
                });
            }

            return result;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var currentUserId = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

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

            return result.Succeeded;
        }

        public async Task<bool> ChangeUserRoleAsync(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return false;

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin") && role != "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                    return false;
            }

            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);

            var result = await _userManager.AddToRoleAsync(user, role);

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
                    UserName = c.User.FullName,
                    UserEmail = c.User.Email!,
                    FileName = fileName,
                    UploadDate = c.UploadedAt
                };
            }).ToList();
        }

        public async Task<bool> DeleteCVAsync(int id)
        {
            var cv = await _context.CVs
                .FirstOrDefaultAsync(x => x.CVId == id);

            if (cv == null)
                return false;

            _context.CVs.Remove(cv);

            await _context.SaveChangesAsync();

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
        public async Task<List<UserManagementDto>> GetUserManagement()
        {
            var users = await _userManager.Users.ToListAsync();

            var result = new List<UserManagementDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserManagementDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    CareerGoal = user.CareerGoal,

                    Role = roles.FirstOrDefault() ?? "User",

                    HasCv = await _context.CVs
                        .AnyAsync(c => c.UserId == user.Id),

                    InterviewsCount = await _context.InterviewSessions
                        .CountAsync(i => i.UserId == user.Id),

                    // For now
                    Plan = "Free",
                    PaymentStatus = "Free",
                    AmountPaid = 0,

                    CreatedAt = user.CreatedAt
                });
            }

            return result;
        }
    }
}

