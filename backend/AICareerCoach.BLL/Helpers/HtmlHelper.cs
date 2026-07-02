using System.Net;
using System.Text.RegularExpressions;

namespace AICareerCoach.BLL.Helpers
{
    public static partial class HtmlHelper
    {
        [GeneratedRegex(@"<[^>]*>")]
        private static partial Regex HtmlTagRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"[.\s]*\.{3,}[.\s]*")]
        private static partial Regex EllipsisRegex();

        public static string StripHtml(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var noTags = HtmlTagRegex().Replace(input, " ");
            var decoded = WebUtility.HtmlDecode(noTags);
            var noEllipsis = EllipsisRegex().Replace(decoded, " ");
            var collapsed = WhitespaceRegex().Replace(noEllipsis, " ");

            return collapsed.Trim();
        }
    }
}
