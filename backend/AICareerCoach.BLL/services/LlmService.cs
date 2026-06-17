using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class LlmService : ILlmService
    {
        private readonly AICareerCoachDbContext _aICareerCoachDbContext;
        private readonly ICVService _cVService;
        private readonly IPdfExtractorService _pdfExtractorService;
        private readonly ICvFeedbackService _cvFeedbackService;
        public LlmService(AICareerCoachDbContext aICareerCoachDbContext,ICVService cVService,IPdfExtractorService pdfExtractorService,ICvFeedbackService cvFeedbackService)
        {
            _aICareerCoachDbContext = aICareerCoachDbContext;
            _cVService = cVService;
            _pdfExtractorService = pdfExtractorService;
            _cvFeedbackService = cvFeedbackService;
            
        }
        public async Task<CvFeedbackDto> GetCvFeedbackAsync(Stream filestrem, string userid)
        {
            var filehashing = SHA256.Create();
            var hashBytes = await filehashing.ComputeHashAsync(filestrem);
            var filehasingresult = Convert.ToHexString(hashBytes);
            var cv = await _aICareerCoachDbContext.CVs.FirstOrDefaultAsync(c => c.filehashing == filehasingresult && c.UserId == userid);
            if (cv == null)
            {
                var cvextraction = await _pdfExtractorService.ExtractTextAsync(filestrem);
                //await _cVService.UploadCVAsync(filestrem, filehasingresult, userid);
                var newcv = new CV
                {
                    FilePath = "", // You can set this to the actual file path if you are saving the file
                    UserId = userid,
                    filehashing = filehasingresult,
                    Extracteddata = cvextraction,
                    UploadedAt = DateTime.UtcNow
                };
               await _aICareerCoachDbContext.AddAsync(newcv);
               await _aICareerCoachDbContext.SaveChangesAsync();
               var feedback= await _cvFeedbackService.GetFeedbackAsync(cvextraction);
                var newfeedback = new AiFeedbackCache
                {
                    Cvid = newcv.CVId,
                    FeedbackJson = System.Text.Json.JsonSerializer.Serialize(feedback),
                    CreatedAt = DateTime.UtcNow

                };
                await _aICareerCoachDbContext.AddAsync(newfeedback);
                await _aICareerCoachDbContext.SaveChangesAsync();
                return feedback;

            }
            var id = cv.CVId;
            var x=  _aICareerCoachDbContext.AiFeedbackCaches.FirstOrDefault(f => f.Cvid==id);
            return JsonSerializer.Deserialize<CvFeedbackDto>(
                    x.FeedbackJson)!;
        }
        
    }
}
