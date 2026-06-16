namespace AICareerCoach.BLL.Services.Interfaces
{
    public interface IPdfExtractorService
    {
        Task<string> ExtractTextAsync(Stream pdfStream);
    }
}