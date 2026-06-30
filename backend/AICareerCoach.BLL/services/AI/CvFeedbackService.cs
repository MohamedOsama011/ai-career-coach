using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AICareerCoach.BLL.Interfaces.AI;

namespace AICareerCoach.BLL.Services.AI
{
    public class CvFeedbackService : ICvFeedbackService
    {
        private readonly AICareerCoachDbContext _context; 
        private readonly IPdfExtractorService _pdfExtractor;
        private readonly ILlmService _llmService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CvFeedbackService> _logger;
        public CvFeedbackService(
            AICareerCoachDbContext context,
            IPdfExtractorService pdfExtractor,
            ILlmService llmService,
            IWebHostEnvironment env,
            ILogger<CvFeedbackService> logger)
        {
            _context = context;
            _pdfExtractor = pdfExtractor;
            _llmService = llmService;
            _env = env;
            _logger = logger;

        }

        public async Task<CvFeedbackDto> GetFeedbackAsync(string userId)
        {
            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync()
                ?? throw new Exception("No CV found. Please upload your CV first.");

            var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", "cvs", cv.FilePath);

            string cvText;
            if (string.IsNullOrWhiteSpace(cv.ExtractedData))
            {
                cvText = _pdfExtractor.ExtractText(fullPath);
                cv.ExtractedData = cvText;
                await _context.SaveChangesAsync();
            }
            else
            {
                cvText = cv.ExtractedData;
            }

            var cvHash = ComputeHash(cvText);

            var cached = await _context.AiFeedbackCaches
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CvHash == cvHash);

            if (cached != null)
            {
                _logger.LogInformation("Returning cached feedback for user {UserId}", userId);
                var cachedResult = JsonSerializer.Deserialize<CvFeedbackDto>(cached.FeedbackJson)!;
                cachedResult.FromCache = true;
                return cachedResult;
            }

            _logger.LogInformation("Calling LLM for user {UserId}", userId);
            var feedback = await _llmService.GetCvFeedbackAsync(cvText);
            feedback.FromCache = false;

            if (feedback.OverallScore > 0) 
            {
                var oldCache = await _context.AiFeedbackCaches.Where(c => c.UserId == userId).ToListAsync();
                _context.AiFeedbackCaches.RemoveRange(oldCache);

                _context.AiFeedbackCaches.Add(new AiFeedbackCache
                {
                    UserId = userId,
                    CvHash = cvHash,
                    FeedbackJson = JsonSerializer.Serialize(feedback),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return feedback;
        }

        private static string ComputeHash(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = MD5.HashData(bytes);
            return Convert.ToHexString(hash);

        }
    }
}
