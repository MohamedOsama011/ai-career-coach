using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface ILlmExplanationService
    {
        Task<Dictionary<int, JobExplanationDto>> GenerateExplanationsAsync(string cvText, List<Job> topJobs);
    }
}
