using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AICareerCoach.API.Reports
{
    public class MockInterviewReport : IDocument
    {
        private readonly string _userName;
        private readonly string _feedback;

        public MockInterviewReport(string userName, string feedback)
        {
            _userName = userName;
            _feedback = feedback;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Mock Interview Report")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"User: {_userName}").Bold();
                    col.Item().Text(_feedback);
                });
            });
        }
    }
}