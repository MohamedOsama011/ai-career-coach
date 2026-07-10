using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class UpdateJobDto
    {
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public string Location { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public bool? IsRemote { get; set; }
        public string? ExternalUrl { get; set; }
    }
}
