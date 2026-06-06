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
            int userId);

        List<CV> GetUserCVs(int userId);

        Task DeleteCV(int cvId);
    }
}
