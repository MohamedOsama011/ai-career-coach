using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class Subscription
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }


        public virtual ICollection<UserSubscription> Subscriptions { get; set; }=new HashSet<UserSubscription>();
    }
}
