using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AICareerCoach.API.Reports
{
    public class CareerRoadmapReport : IDocument
    {
        private readonly string _userName;
        private readonly string _roadmap;

        public CareerRoadmapReport(string userName, string roadmap)
        {
            _userName = userName;
            _roadmap = roadmap;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Career Roadmap Report")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"User: {_userName}").Bold();
                    col.Item().Text(_roadmap);
                });
            });
        }
    }
}