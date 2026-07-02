using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IPdfExtractorService _pdfExtractor;
        private readonly IWebHostEnvironment _env;


        public CVService(
            IBaserepo<CV> cvRepo,
            IFileStorageService fileStorage,
            AICareerCoachDbContext _context,
            ILogger<CVService> logger,
            IPdfExtractorService pdfExtractor,
            IWebHostEnvironment env)
        {
            _cvRepo = cvRepo;
            _fileStorage = fileStorage;
            context= _context;
            _logger = logger;
            _pdfExtractor = pdfExtractor;
            _env = env;
        }

        public async Task<UploadCVResult> UploadCVAsync(
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
                return new UploadCVResult(existing, IsNew: false);

            using var uploadStream = new MemoryStream(fileBytes);
            var savedPath = await _fileStorage.SaveFileAsync(uploadStream, fileName);

            var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", "cvs", savedPath);

            string cvText;
            try
            {
                cvText = _pdfExtractor.ExtractText(fullPath);
            }
            catch (Exception ex)
            {
                _fileStorage.DeleteFile(savedPath);
                _logger.LogError(ex, "PDF text extraction failed for user {UserId} at {Path}.", userId, fullPath);
                throw new Exception(
                    "Could not extract text from the uploaded PDF. Please ensure it's a valid, text-based PDF (not a scanned image).");
            }

            var cv = new CV
            {
                UserId = userId,
                FilePath = savedPath,
                FileHash = fileHash,
                UploadedAt = DateTime.UtcNow,
                ExtractedData = cvText
            };

            _logger.LogInformation("Extracted {Length} chars from CV for user {UserId}.", cvText.Length, userId);

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

            return new UploadCVResult(cv, IsNew: true);
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
