using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(
            Stream stream,
            string fileName);

        void DeleteFile(string path);
    }
}
