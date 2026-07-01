using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class SubscriptionService : ISubsription
    {
        private readonly IBaserepo<Subscription> baserepo;
        private readonly AICareerCoachDbContext aICareerCoachDbContext;
        public SubscriptionService(IBaserepo<Subscription> _baserepo,AICareerCoachDbContext _aICareerCoachDbContext)
        {
            baserepo = _baserepo;
            aICareerCoachDbContext = _aICareerCoachDbContext;
        }


        public async Task<Generalresponse> Getall()
        {
            var response= new Generalresponse();
            List<Subscription>? list =  baserepo.Getall();

            if (list?.Count <= 0)
            {
                response.Data = "there isn't any subscription yet";
                response.Success = false;
            }
            else
            { 
                response.Data = list;
                response.Success = true;
            }
            return response;
        }

       public async Task<Generalresponse> Get(string id)
        {
            var response=new Generalresponse();
            var subscription = baserepo.GetbyId(int.Parse(id));
            if(subscription == null)
            {
                response.Success = false;
                response.Data = "no such subscription";

            }
            else
            {
                response.Success=true;
                response.Data = subscription;
            }
            return response;
        }
       public void  CreateSubscription(SubscriptionDTO subscription)
        {
            var newsub=new Subscription();
            newsub.Name = subscription.Name;
            newsub.Price= subscription.Price;
            baserepo.Add(newsub);
            aICareerCoachDbContext.SaveChanges();

        }

        public void DeleteSubscription(Subscription subscription)
        {
            baserepo.Delete(subscription);
            aICareerCoachDbContext.SaveChanges();

        }
        public async Task<Generalresponse> UpdateSubscription(SubscriptionDTO subscription,string id)
        {
            var response = new Generalresponse();
            var sub=await aICareerCoachDbContext.Subscriptions.FirstOrDefaultAsync(x=>x.Id.ToString()==id);
            if(sub==null)
            {
                return new Generalresponse
                {
                    Success = false,
                    Data= "subscription not found"
                };
            }

                sub.Price= subscription.Price;
                sub.Name= subscription.Name;
            baserepo.Update(sub);
            aICareerCoachDbContext.SaveChanges();
            return new Generalresponse
            {
                Success = true,
                Data = "updated successfuly"
            };

        }
        //public async Task<Generalresponse> UpdateSubscription( string id)
        //{ 
        //    var response = new Generalresponse();
        //    var sub = await aICareerCoachDbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id.ToString() == id);
        //    if (sub == null)
        //    {
        //        response.Success = false;
        //        response.Data = "subscription not found";
        //    }
        //    else
        //    {
        //        var newsub=new SubscriptionDTO();
        //        newsub.Price= sub.Price;
        //        newsub.Name= sub.Name;
        //        Update(newsub,id);
        //        response.Success = true;
        //        response.Data = "updated asuccessfuly";
        //    }
        //    return response;
        //}

        
    }
}
