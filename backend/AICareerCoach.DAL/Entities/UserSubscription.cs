using AICareerCoach.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public string? Userid { get; set; }
        public int? Subscriptionid { get; set; }
        public bool Isactive { get; set; }=false;
        public DateTime? StartDate { get; set; }
        public DateTime? Enddate { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; } = "pending";


        public virtual User? User { get; set; }
        public virtual Subscription? Subscription { get; set; }
        public virtual ICollection<Payment>? Payments { get; set; }= new List<Payment>();


    }
}
