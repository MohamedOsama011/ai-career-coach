using AICareerCoach.BLL.DTOs.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces.External
{
    public interface IJobProvider
    {
        Task<List<JobFetchResultDto>> FetchJobsAsync(string country, int maxPages, CancellationToken ct);
    }
}
