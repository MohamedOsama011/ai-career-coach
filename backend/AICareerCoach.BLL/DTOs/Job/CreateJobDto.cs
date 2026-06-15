using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class CreateJobDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Company { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public string Location { get; set; } = string.Empty;
        public decimal Salary { get; set; }
    }
}
