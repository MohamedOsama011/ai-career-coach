using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AICareerCoach.BLL.Services.Pdf
{
    public class PdfReportService : IPdfReportService
    {
        public byte[] GenerateCvReport(CvFeedbackDto report)
        {
            if (report == null)
                throw new Exception("Report is null");

            report.Strengths ??= new();
            report.MissingKeywords ??= new();
            report.Suggestions ??= new();
            report.OverallSummary ??= "";

            return Document.Create(container =>
            {
            container.Page(page =>
            {
            page.Margin(30);

            page.Header().Column(header =>
            {
                header.Item()
                    .Text("AI Career Coach")
                    .FontSize(26)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                header.Item()
                    .Text("CV Analysis Report")
                    .FontSize(18)
                    .SemiBold();

                header.Item().LineHorizontal(1);
            });

            page.Content().Column(column =>
            {
            column.Spacing(18);

            // =======================
            // SCORE CARD
            // =======================

            column.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(15)
                .Column(box =>
                {
                    box.Item()
                        .Text($"Overall Score : {report.OverallScore}/100")
                        .Bold()
                        .FontSize(20)
                        .FontColor(Colors.Green.Darken2);

                    box.Item()
                        .Text($"Generated At : {report.GeneratedAt:g}");

                    box.Item().PaddingTop(10);

                    box.Item().Text($"Keyword Match : {report.KeywordMatch}%");
                    box.Item().Text($"Impact Statements : {report.ImpactStatements}%");
                    box.Item().Text($"Formatting : {report.Formatting}%");
                    box.Item().Text($"Leadership Signals : {report.LeadershipSignals}%");
                });

            // =======================
            // SUMMARY
            // =======================

            column.Item()
                .Text("Overall Summary")
                .Bold()
                .FontSize(16);

            column.Item()
                .Text(report.OverallSummary);

            // =======================
            // STRENGTHS
            // =======================

            column.Item()
                .PaddingTop(10)
                .Text("Key Strengths")
                .Bold()
                .FontSize(16);

            foreach (var strength in report.Strengths)
            {
                column.Item()
                    .Text($"✓ {strength}");
            }

            // =======================
            // MISSING KEYWORDS
            // =======================

            column.Item()
                .PaddingTop(10)
                .Text("Missing Keywords")
                .Bold()
                .FontSize(16);

            foreach (var keyword in report.MissingKeywords)
            {
                column.Item()
                    .Text($"• {keyword}");
            }

            // =======================
            // SUGGESTIONS
            // =======================

            column.Item()
                .PaddingTop(10)
                .Text("Suggested Improvements")
                .Bold()
                .FontSize(16);

            foreach (var s in report.Suggestions)
            {
                column.Item()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(12)
                    .Column(box =>
                    {
                    box.Item()
                                    .Text(s.Category.ToUpper())
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);
                        box.Item()
        .PaddingTop(5)
        .Text($"Issue: {s.Issue}");

                        box.Item()
                            .PaddingTop(3)
                            .Text($"Recommendation: {s.Recommendation}");

                        box.Item()
                            .PaddingTop(3)
                            .Text($"Priority: {s.Priority}")
                            .Bold()
                            .FontColor(
                                s.Priority == "High"
                                    ? Colors.Red.Darken2
                                    : s.Priority == "Medium"
                                        ? Colors.Orange.Darken2
                                        : Colors.Green.Darken2
                            );
                    });
                }

                // =======================
                // END OF REPORT
                // =======================

                column.Item()
                    .PaddingTop(15)
                    .LineHorizontal(1);

                column.Item()
                    .AlignCenter()
                    .Text("End of CV Analysis Report")
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Generated by ");
                        text.Span("AI Career Coach").Bold();
                    });
            });
            })
            .GeneratePdf();
        }

        public byte[] GenerateRoadmapReport(RoadmapDto roadmap)
        {
            if (roadmap == null)
                throw new Exception("Roadmap is null");

            roadmap.Steps ??= new();
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Text("AI Career Coach")
                            .FontSize(26)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        header.Item()
                            .Text("Career Roadmap Report")
                            .FontSize(18)
                            .SemiBold();

                        header.Item().LineHorizontal(1);
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        column.Item()
                            .Text($"Track: {roadmap.Track}")
                            .Bold()
                            .FontSize(18);

                        column.Item()
                            .Text($"Roadmap Title: {roadmap.Title}")
                            .FontSize(16);

                        column.Item()
                            .PaddingTop(10)
                            .Text("Description")
                            .Bold()
                            .FontSize(16);

                        column.Item()
                            .Text(roadmap.Description);

                        column.Item()
                            .PaddingTop(20)
                            .Text("Learning Steps")
                            .Bold()
                            .FontSize(18);

                        foreach (var step in roadmap.Steps.OrderBy(x => x.OrderIndex))
                        {
                            column.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(12)
                                .Column(box =>
                                {
                                    box.Item()
                                        .Text($"{step.OrderIndex}. {step.Title}")
                                        .Bold()
                                        .FontSize(15)
                                        .FontColor(Colors.Blue.Darken2);

                                    box.Item()
                                        .Text($"Level: {step.Level}");

                                    box.Item()
                                        .PaddingTop(5)
                                        .Text(step.Description);

                                    if (step.Resources != null && step.Resources.Any())
                                    {
                                        box.Item()
                                            .PaddingTop(8)
                                            .Text("Resources")
                                            .Bold();

                                        foreach (var resource in step.Resources)
                                        {
                                            box.Item()
                                                .Text($"• {resource}");
                                        }
                                    }
                                });
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated by ");
                            text.Span("AI Career Coach").Bold();
                        });
                });
            })
.GeneratePdf();
        }
    }
}