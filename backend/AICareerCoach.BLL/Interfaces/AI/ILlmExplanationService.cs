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
        Task<Dictionary<int, string>> GenerateExplanationsAsync(string cvText, List<Job> topJobs);
    }
}
