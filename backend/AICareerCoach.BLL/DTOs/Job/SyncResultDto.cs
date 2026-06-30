using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Job
{
    public class SyncResultDto
    {
        public int Fetched { get; set; }
        public int New { get; set; }
        public int Skipped { get; set; }
        public int Embedded { get; set; }
        public int Errors { get; set; }
        public DateTime SyncedAt { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}
