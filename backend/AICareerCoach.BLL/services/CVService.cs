using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services
{
    public class CVService : ICVService
    {
        private readonly IBaserepo<CV> _cvRepo;
        private readonly IFileStorageService _fileStorage;

        private readonly AICareerCoachDbContext context;
        private readonly ILogger<CVService> _logger;


        public CVService(
            IBaserepo<CV> cvRepo,
            IFileStorageService fileStorage,
            AICareerCoachDbContext _context,
            ILogger<CVService> logger)
        {
            _cvRepo = cvRepo;
            _fileStorage = fileStorage;
            context= _context;
            _logger = logger;
        }

        public async Task<CV> UploadCVAsync(
            Stream fileStream,
            string fileName,
            string userId)
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var fileHash = Convert.ToHexString(MD5.HashData(fileBytes));

            var existing = await context.CVs
                .FirstOrDefaultAsync(c => c.UserId == userId && c.FileHash == fileHash);

            if (existing != null)
                return existing;

            using var uploadStream = new MemoryStream(fileBytes);
            var savedPath = await _fileStorage.SaveFileAsync(uploadStream, fileName);

            var cv = new CV
            {
                UserId = userId,
                FilePath = savedPath,
                FileHash = fileHash,
                UploadedAt = DateTime.UtcNow
            };

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await context.Database.BeginTransactionAsync();

                context.CVs.Add(cv);
                await context.SaveChangesAsync();

                int jobsDeleted = await context.JobRecommendationCaches
                    .Where(j => j.UserId == userId)
                    .ExecuteDeleteAsync();

                int roadmapsDeleted = await context.UserRoadmaps
                    .Where(r => r.UserId == userId)
                    .ExecuteDeleteAsync();

                await tx.CommitAsync();

                _logger.LogWarning(
                    "New CV uploaded by {UserId} — invalidated {Jobs} job-rec cache rows and {Roadmaps} roadmap rows.",
                    userId, jobsDeleted, roadmapsDeleted);
            });

            return cv;
        }

        public List<CV> GetUserCVs(string userId)
        {
            return _cvRepo
                .Getall()!
                .Where(c => c.UserId == userId)
                .ToList();
        }

        public void DeleteCV(int cvId)
        {
            var cv = _cvRepo.GetbyId(cvId);

            if (cv == null)
                throw new Exception("CV Not Found");

            _fileStorage.DeleteFile(cv.FilePath);

            _cvRepo.Delete(cv);
        }
    }
}
