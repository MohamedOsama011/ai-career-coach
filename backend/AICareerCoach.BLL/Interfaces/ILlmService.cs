
using AICareerCoach.BLL.DTOs.CV;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ILlmService
    {
        Task<CvFeedbackDto> GetCvFeedbackAsync(string cvText);
    }
}
