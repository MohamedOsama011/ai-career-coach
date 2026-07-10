using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.BLL.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AICareerCoachDbContext _context;

        public SubscriptionService(AICareerCoachDbContext context)
        {
            _context = context;
        }

        public async Task<GeneralResponse<List<Subscription>>> GetAllSubscriptionsAsync()
        {
            var list = await _context.Subscriptions.ToListAsync();
            return new GeneralResponse<List<Subscription>>
            {
                Data = list.Count > 0 ? list : null,
                Success = list.Count > 0,
            };
        }

        public async Task<GeneralResponse<Subscription>> GetSubscriptionByIdAsync(string id)
        {
            var subscription = await _context.Subscriptions.FindAsync(int.Parse(id));
            return new GeneralResponse<Subscription>
            {
                Success = subscription != null,
                Data = subscription,
            };
        }

        public async Task CreateSubscriptionAsync(SubscriptionDto dto)
        {
            var subscription = new Subscription
            {
                Name = dto.Name,
                Price = dto.Price,
                DurationMonths = dto.DurationMonths,
                LimitsJson = dto.LimitsJson,
            };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<GeneralResponse<string>> DeleteSubscriptionAsync(string id)
        {
            var sub = await _context.Subscriptions
                .Include(x => x.UserSubscriptions)
                .FirstOrDefaultAsync(x => x.Id == int.Parse(id));
            if (sub == null)
            {
                return new GeneralResponse<string> { Success = false, Data = "subscription not found" };
            }

            if (sub.UserSubscriptions != null && sub.UserSubscriptions.Count > 0)
            {
                return new GeneralResponse<string> { Success = false, Data = $"cannot delete plan: {sub.UserSubscriptions.Count} active subscriber(s)" };
            }

            _context.Subscriptions.Remove(sub);
            await _context.SaveChangesAsync();
            return new GeneralResponse<string> { Success = true, Data = "deleted successfully" };
        }

        public async Task<GeneralResponse<string>> UpdateSubscriptionAsync(SubscriptionDto dto, string id)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(x => x.Id.ToString() == id);
            if (sub == null)
            {
                return new GeneralResponse<string> { Success = false, Data = "subscription not found" };
            }

            sub.Price = dto.Price;
            sub.Name = dto.Name;
            sub.DurationMonths = dto.DurationMonths;
            sub.LimitsJson = dto.LimitsJson;
            await _context.SaveChangesAsync();

            return new GeneralResponse<string> { Success = true, Data = "updated successfully" };
        }
    }
}
