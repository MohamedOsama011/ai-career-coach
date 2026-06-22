using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AICareerCoach.API.Reports
{
    public class CvAnalysisReport : IDocument
    {
        private readonly string _userName;
        private readonly string _analysis;

        public CvAnalysisReport(string userName, string analysis)
        {
            _userName = userName;
            _analysis = analysis;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("CV Analysis Report")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"User: {_userName}").Bold();
                    col.Item().Text(_analysis);
                });
            });
        }
    }
}