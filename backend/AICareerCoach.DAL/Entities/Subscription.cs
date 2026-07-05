using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime Createdatat { get; set; }
        
        public DateTime?updatedat { get; set; }

        public string Description { get; set; }



        public virtual ICollection<UserSubscription>? Subscriptions { get; set; }=new HashSet<UserSubscription>();
    }
}
