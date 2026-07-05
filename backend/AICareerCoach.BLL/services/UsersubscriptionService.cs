using AICareerCoach.BLL.DTOs;
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

namespace AICareerCoach.BLL.services
{
    public class UsersubscriptionService : Iusersubscription
    {
        AICareerCoachDbContext context;
        private readonly UserManager<User> userManager;

        public UsersubscriptionService(AICareerCoachDbContext _context, UserManager<User> _usermanager)
        {
            context = _context;
            userManager = _usermanager;
        }

        public async Task<List<UsersubresponseDTO>> getallbyuserid(string userid)
        {

            var user = await userManager.FindByIdAsync(userid);
            if (user == null)
            {
                return new List<UsersubresponseDTO>();
            }
            else
            {
                var subscriptions = await context.UserSubscriptions
        .Where(u => u.Userid == userid)
        .Include(u => u.Subscription)
        .Select(u => new UsersubresponseDTO
        {
            SubscriptionName = u.Subscription!.Name,
            Status = u.Status!,
            StartDate = u.StartDate,
            EndDate = u.Enddate
        })
        .ToListAsync();

                return subscriptions;
            }
        }
    }
}