using AICareerCoach.BLL.DTOs.CV;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IPdfReportService
    {
        byte[] GenerateCvAnalysisReport(CvFeedbackDto feedback);
    }
}