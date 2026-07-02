using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class ApprovedWebhookDto
    {
        public string transactionId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string status { get; set; }
        public string reason { get; set; }
        public DateTime approvedAt { get; set; }
    
        
    
    }
}
