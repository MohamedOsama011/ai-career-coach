using AICareerCoach.BLL.DTOs.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IJobRecommendationService
    {
        Task IndexJobsAsync();
        Task<JobRecommendationResultDto> GetRecommendationsAsync(string userId);
    }
}
