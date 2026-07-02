using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class SyncStatusDto
    {
        public DateTime? LastSyncAt { get; set; }
        public int? LastSyncNew { get; set; }
        public int? LastSyncErrors { get; set; }
        public bool Enabled { get; set; }
        public double IntervalHours { get; set; }
    }
}
