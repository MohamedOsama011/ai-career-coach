using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using QuestPDF.Fluent;

namespace AICareerCoach.BLL.Services.Pdf
{
    public class PdfReportService : IPdfReportService
    {
        public byte[] GenerateCvAnalysisReport(CvFeedbackDto feedback)
        {
            var document = new CvAnalysisReportDocument(feedback);

            return document.GeneratePdf();
        }
    }
}