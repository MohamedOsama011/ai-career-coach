using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.BLL.services.FileStorage;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;

namespace AICareerCoach.BLL.services.cv
{
    public class CVService : ICVService
    {
        private readonly IBaserepo<CV> _cvRepo;
        private readonly IFileStorageService _fileStorage;

        public CVService(
            IBaserepo<CV> cvRepo,
            IFileStorageService fileStorage)
        {
            _cvRepo = cvRepo;
            _fileStorage = fileStorage;
        }

        public async Task<CV> UploadCVAsync(
            Stream fileStream,
            string fileName,
            int userId)
        {
            var savedPath =
                await _fileStorage.SaveFileAsync(
                    fileStream,
                    fileName);

            var cv = new CV
            {
                UserId = userId,
                FilePath = savedPath,
                UploadedAt = DateTime.UtcNow
            };

            _cvRepo.Add(cv);

            return cv;
        }

        public List<CV> GetUserCVs(int userId)
        {
            return _cvRepo
                .Getall()!
                .Where(c => c.UserId == userId)
                .ToList();
        }

        public  Task DeleteCV(int cvId)
        {
            var cv = _cvRepo.GetbyId(cvId);

            if (cv == null)
                throw new Exception("CV Not Found");

            _fileStorage.DeleteFile(cv.FilePath);

            _cvRepo.Delete(cv);

            return Task.CompletedTask;
        }
    }
}
