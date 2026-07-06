using AICareerCoach.BLL.DTOs.User;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly AICareerCoachDbContext _context;

        public UserService(UserManager<User> userManager, AICareerCoachDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            var cvCount = await _context.Set<CV>()
                .CountAsync(c => c.UserId == user.Id);

            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                CareerGoal = user.CareerGoal,
                CreatedAt = user.CreatedAt,
                CvCount = cvCount,
                Roles = roles,
                HasCV = cvCount > 0
            };
        }
    }
}
