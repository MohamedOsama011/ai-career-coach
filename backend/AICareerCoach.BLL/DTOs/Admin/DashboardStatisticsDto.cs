using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Admin
{
    namespace AICareerCoach.BLL.DTOs
    {
        public class DashboardStatisticsDto
        {
            public int Users { get; set; }

            public int Admins { get; set; }

            public int CVs { get; set; }

            public int Interviews { get; set; }

            public decimal Revenue { get; set; }

            public int Payments { get; set; }

            public int SuccessfulPayments { get; set; }

            public int PendingPayments { get; set; }
        }
    }
}
