using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class JobRecommendationResultDto
    {
        public string UserId { get; set; } = string.Empty;

        public List<JobRecommendationDto> Recommendations { get; set; } = new();

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
