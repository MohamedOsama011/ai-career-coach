using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.services.cv
{
    public interface ICVService
    {
        Task<CV> UploadCVAsync(
            Stream fileStream,
            string fileName,
            string userId);

        List<CV> GetUserCVs(string userId);

        void DeleteCV(int cvId);
    }
}
