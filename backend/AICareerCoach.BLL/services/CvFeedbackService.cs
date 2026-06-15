using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class CvFeedbackService : ICvFeedbackService
    {
        public Task<CvFeedbackDto> GetFeedbackAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
