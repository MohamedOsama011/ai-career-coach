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
        public int Id { get; set; }
        public string? Status { get; set; }
        public int? Usersubscriptionid { get; set; }
        public decimal Amount { get; set; }
        public string? invoicenumber { get; set; }//=usersubscriotionid
        public DateTime? paidat { get; set; }


        public string? intentkey { get; set; } = null;//transactionkey
        
        public string? PaymentMethod { get; set; }
        public string? transactionid { get; set; }
        public string? transactionkey { get; set; }



        public string? referenceNumber { get; set; }

        public string? invoiceid{ get; set; }
        public string? invoicekey { get; set; } = null;




        public virtual UserSubscription? UserSubscription { get; set; }



    }
}
