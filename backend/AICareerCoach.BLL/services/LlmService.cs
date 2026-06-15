using OpenAI;
using OpenAI.Chat;
using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.services
{
    public class LlmService : ILlmService
    {
        public Task<CvFeedbackDto> GetCvFeedbackAsync(string cvText)
        {
            throw new NotImplementedException();
        }
    }
}
