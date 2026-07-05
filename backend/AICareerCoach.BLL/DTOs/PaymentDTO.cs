using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs
{
    public  class PaymentDTO
    {
        public string SubscriptionName { get; set; }
        public string SubscriptionStatus { get; set; }
        public string PaymentStatus { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
