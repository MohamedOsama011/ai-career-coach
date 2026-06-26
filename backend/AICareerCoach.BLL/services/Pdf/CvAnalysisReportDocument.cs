using AICareerCoach.BLL.DTOs.CV;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AICareerCoach.BLL.Services.Pdf
{
    public class CvAnalysisReportDocument : IDocument
    {
        private readonly CvFeedbackDto _feedback;

        public CvAnalysisReportDocument(CvFeedbackDto feedback)
        {
            _feedback = feedback;
        }

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("CV Analysis Report")
                    .FontSize(24)
                    .Bold();

                page.Content()
                    .PaddingVertical(20)
                    .Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Overall Score : {_feedback.OverallScore}/100")
                            .FontSize(18)
                            .Bold();

                        col.Item().Text($"Keyword Match : {_feedback.KeywordMatch}%");

                        col.Item().Text($"Impact Statements : {_feedback.ImpactStatements}%");

                        col.Item().Text($"Formatting : {_feedback.Formatting}%");

                        col.Item().Text($"Leadership Signals : {_feedback.LeadershipSignals}%");

                        col.Item().PaddingTop(15);

                        col.Item().Text("Overall Summary")
                            .FontSize(18)
                            .Bold();

                        col.Item().Text(_feedback.OverallSummary);

                        col.Item().PaddingTop(15);

                        col.Item().Text("Strengths")
                            .FontSize(18)
                            .Bold();

                        foreach (var item in _feedback.Strengths)
                        {
                            col.Item().Text($"• {item}");
                        }

                        col.Item().PaddingTop(15);

                        col.Item().Text("Missing Keywords")
                            .FontSize(18)
                            .Bold();

                        foreach (var item in _feedback.MissingKeywords)
                        {
                            col.Item().Text($"• {item}");
                        }

                        col.Item().PaddingTop(15);

                        col.Item().Text("Suggestions")
                            .FontSize(18)
                            .Bold();

                        foreach (var s in _feedback.Suggestions)
                        {
                            col.Item().Column(c =>
                            {
                                c.Item().Text($"Category : {s.Category}").Bold();
                                c.Item().Text($"Priority : {s.Priority}");
                                c.Item().Text($"Issue : {s.Issue}");
                                c.Item().Text($"Recommendation : {s.Recommendation}");
                                c.Item().PaddingBottom(10);
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text($"Generated : {DateTime.Now:dd/MM/yyyy HH:mm}");
            });
        }
    }
}