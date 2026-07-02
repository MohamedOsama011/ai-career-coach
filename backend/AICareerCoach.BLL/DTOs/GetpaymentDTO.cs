using AICareerCoach.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs
{
    public class GetpaymentDTO
    {
        public string Status { get; set; }
        public string Usersubscriptionid { get; set; }
        public decimal Amount { get; set; }
        public string Invoiceid { get; set; } = null;
        public string Paymentprovider { get; set; }


        //public virtual UserSubscription? UserSubscription { get; set; }
    }
}
