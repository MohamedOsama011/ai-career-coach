using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class CancelwebhookDTO
    {

        public string hashKey { get; set; }
        public string paymentMethod { get; set; }
        public string referenceId { get; set; }
        public string status { get; set; }
        public string pay_load { get; set; }
        public string transactionKey { get; set; }
        public int transactionId { get; set; }

    }
}
