using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IJobService
    {
        Task<PagedResult<JobDto>> GetJobsAsync(JobFilterDto filter);
        Task<JobDto> GetByIdAsync(int id);
        Task<JobDto> CreateAsync(CreateJobDto dto);
        Task<JobDto> UpdateAsync(int id, UpdateJobDto dto);
        Task DeleteAsync(int id);
    }
}
