using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class JobDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public string Location { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime PostedAt { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? ExternalUrl { get; set; }
        public string? ContractType { get; set; }
        public bool IsRemote { get; set; }
        public string? Category { get; set; }
        public string? Source { get; set; }
    }
}
