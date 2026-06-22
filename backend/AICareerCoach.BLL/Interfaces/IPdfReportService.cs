namespace AICareerCoach.BLL.Services.Interfaces
{
    public interface IPdfReportService
    {
        byte[] GenerateCvReport(string userName, string cvAnalysis);

        byte[] GenerateRoadmapReport(string userName, string roadmapText);

        byte[] GenerateInterviewReport(string userName, string interviewResult);
    }
}