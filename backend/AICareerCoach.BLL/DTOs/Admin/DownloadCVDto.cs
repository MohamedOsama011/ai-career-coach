using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class DownloadCVDto
    {
        public string FilePath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public bool Exists(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}
