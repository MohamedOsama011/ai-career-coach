using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using AICareerCoach.BLL.Services.Interfaces;

namespace AICareerCoach.BLL.Services
{
    public class PdfExtractorService : IPdfExtractorService
    {
        public string ExtractText(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CV file not found.", filePath);

            var sb = new StringBuilder();

            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords();
                sb.AppendLine(string.Join(" ", words.Select(w => w.Text)));
            }

            var text = sb.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Could not extract text from PDF. Make sure it's not a scanned image.");

            return text;
        }
    }
}