using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class UserManagementDto
    {
        public string Id { get; set; } = default!;

        public string FullName { get; set; } = default!;

        public string Email { get; set; } = default!;

        public string Role { get; set; } = default!;

        public string? CareerGoal { get; set; }

        public bool HasCv { get; set; }

        public int InterviewsCount { get; set; }

        // Subscription
        public string Plan { get; set; } = "Free";

        public string PaymentStatus { get; set; } = "Free";

        public decimal AmountPaid { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
