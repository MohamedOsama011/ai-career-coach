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

        public async Task<Generalresponse> GetAllSubscriptionsAsync()
        {
            var list = await _context.Subscriptions.ToListAsync();
            return new Generalresponse
            {
                Data = list.Count > 0 ? list : "there isn't any subscription yet",
                Success = list.Count > 0,
            };
        }

        public async Task<Generalresponse> GetSubscriptionByIdAsync(string id)
        {
            var subscription = await _context.Subscriptions.FindAsync(int.Parse(id));
            return new Generalresponse
            {
                Success = subscription != null,
                Data = subscription != null ? (object)subscription : "no such subscription",
            };
        }

        public async Task CreateSubscriptionAsync(SubscriptionDto dto)
        {
            var subscription = new Subscription
            {
                Name = dto.Name,
                Price = dto.Price,
            };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubscriptionAsync(Subscription subscription)
        {
            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task<Generalresponse> UpdateSubscriptionAsync(SubscriptionDto dto, string id)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(x => x.Id.ToString() == id);
            if (sub == null)
            {
                return new Generalresponse { Success = false, Data = "subscription not found" };
            }

            sub.Price = dto.Price;
            sub.Name = dto.Name;
            await _context.SaveChangesAsync();

            return new Generalresponse { Success = true, Data = "updated successfully" };
        }
    }
}
