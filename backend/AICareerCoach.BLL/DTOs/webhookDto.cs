using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs
{
    public class webhookDto
    {
       public string hashKey { get; set; }
        public string invoice_key { get; set; }
        public string payment_method { get; set; }
        public string invoice_status { get; set; }
        public string referenceNumber { get; set; }
        public int invoice_id { get; set; }
        public object? pay_load;
    
    }
}
