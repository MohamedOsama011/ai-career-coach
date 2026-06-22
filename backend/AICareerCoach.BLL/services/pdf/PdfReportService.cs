using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AICareerCoach.BLL.Services.Pdf
{
    public class PdfReportService : IPdfReportService
    {
        public byte[] GenerateCvReport(string userName, string cvAnalysis)
        {
            return BuildDocument("CV Analysis Report", userName, cvAnalysis);
        }

        public byte[] GenerateRoadmapReport(string userName, string roadmapText)
        {
            return BuildDocument("Career Roadmap Report", userName, roadmapText);
        }

        public byte[] GenerateInterviewReport(string userName, string interviewResult)
        {
            return BuildDocument("Mock Interview Report", userName, interviewResult);
        }

        private byte[] BuildDocument(string title, string userName, string content)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.Header()
                        .Text(title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(col =>
                        {
                            col.Item().Text($"User: {userName}")
                                .FontSize(14)
                                .Bold();

                            col.Item().PaddingTop(10).Text(content)
                                .FontSize(12);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:yyyy-MM-dd}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateCvReport(CvFeedbackDto report)
        {
            throw new NotImplementedException();
        }
    }
}