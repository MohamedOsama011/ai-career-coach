using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.DTOs.Roadmap;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IPdfReportService
    {
    byte[] GenerateCvReport(CvFeedbackDto report);
    byte[] GenerateRoadmapReport(RoadmapDto roadmap);
    byte[] GenerateModifiedCvReport(string modifiedText);
    }
}