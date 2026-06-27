using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class Payment
    {
        [Key]
        public string Id { get; set; }
        public string Status { get; set; }
        public string Usersubscriptionid { get; set; }
        public decimal Amount { get; set; }
        public string InternalTransactionid { get; set; }
        public string GatewayTransactionid { get; set; } = null;
        public string Invoiceid { get; set; } = null;
        public string Paymentprovider { get; set; }
        public string InvoiceKey { get; set; }
        public string PaymentMethod { get; set; }

        


        public virtual UserSubscription? UserSubscription { get; set; }



    }
}
