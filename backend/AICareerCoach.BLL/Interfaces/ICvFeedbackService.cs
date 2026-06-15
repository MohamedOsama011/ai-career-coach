using AICareerCoach.BLL.DTOs.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ICvFeedbackService
    {
        Task<CvFeedbackDto> GetFeedbackAsync(string userId);
    }
}
