using AICareerCoach.BLL.DTOs;
using AICareerCoach.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ISubsription
    {
        Task<Generalresponse> Getall();
        Task<Generalresponse> Get(string id);
        void CreateSubscription(SubscriptionDTO subscription);
        void  DeleteSubscription(Subscription subscription);
        Task<Generalresponse> UpdateSubscription(SubscriptionDTO dTO,string id);

    }
}
