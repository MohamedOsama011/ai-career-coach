using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class JobSyncLog
    {
        public int Id { get; set; }
        public DateTime SyncedAt { get; set; }
        public int Fetched { get; set; }
        public int New { get; set; }
        public int Skipped { get; set; }
        public int Embedded { get; set; }
        public int Errors { get; set; }
        public string? ErrorMessages { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
