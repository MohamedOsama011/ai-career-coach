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

        public UsersubscriptionService(AICareerCoachDbContext _context,UserManager<User> _usermanager)
        {
            context = _context;
            userManager = _usermanager;
        }

        public async Task<Generalresponse> getallbyuserid(string userid)
        {
            var response = new Generalresponse();
            var user=await userManager.FindByIdAsync(userid);
            if(user==null)
            {
                response.Success = false;
                response.Data = "user not found";

            }
            else
            {
                var x= await context.UserSubscriptions.Where(x=>x.Userid==userid).ToListAsync();
                if(x.Count<=0)
                {
                    response.Success = true;
                    response.Data = "user hasent any subscription yet";
                }
                else
                {
                    response.Success = true;
                    response.Data = x;
                }
            }
 return response;
        }
    }
}
