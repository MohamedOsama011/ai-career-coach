using AICareerCoach.BLL.DTOs.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IJobSyncService
    {
        Task<SyncResultDto> SyncAsync(CancellationToken ct);
        Task<SyncStatusDto> GetStatusAsync();
    }
}
