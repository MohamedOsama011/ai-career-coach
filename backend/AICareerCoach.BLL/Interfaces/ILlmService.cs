
using AICareerCoach.BLL.DTOs.CV;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ILlmService
    {
        Task<CvFeedbackDto> GetCvFeedbackAsync(string cvText);
    }
}
