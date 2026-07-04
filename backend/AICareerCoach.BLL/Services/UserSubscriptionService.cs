using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.BLL.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly AICareerCoachDbContext _context;

        public UserSubscriptionService(AICareerCoachDbContext context)
        {
            _context = context;
        }

        public async Task<Generalresponse> GetAllByUserIdAsync(string userId)
        {
            var list = await _context.UserSubscriptions
                .Include(x => x.Subscription)
                .Include(x => x.Payments)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            return new Generalresponse
            {
                Data = list,
                Success = true,
            };
        }
    }
}
