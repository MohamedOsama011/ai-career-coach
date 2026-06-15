using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using AICareerCoach.BLL.Services.Interfaces;

namespace AICareerCoach.BLL.Services
{
    public class PdfExtractorService : IPdfExtractorService
    {
        public async Task<string> ExtractTextAsync(Stream pdfStream)
        {
            if (pdfStream == null || pdfStream.Length == 0)
                throw new Exception("PDF file is empty.");

            using var document = PdfDocument.Open(pdfStream);

            var extractedText = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                extractedText.AppendLine(page.Text);
            }

            return CleanText(extractedText.ToString());
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace("\r\n", "\n")
                .Replace("\n\n", "\n")
                .Trim();
        }
    }
}