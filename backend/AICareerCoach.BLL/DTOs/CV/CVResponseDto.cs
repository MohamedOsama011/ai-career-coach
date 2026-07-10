using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.CV
{
    public class CVResponseDto
    {
        public int CVId { get; set; }
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UserId { get; set; }
        public bool IsNew { get; set; }
    }
}
