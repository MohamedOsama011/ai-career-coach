using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class PaymentManagementDto
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string Plan { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime PaymentDate { get; set; }

        public string TransactionId { get; set; }
    }
}
