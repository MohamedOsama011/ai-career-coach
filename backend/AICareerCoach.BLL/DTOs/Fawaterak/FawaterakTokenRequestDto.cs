namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class FawaterakTokenRequestDto
    {
        public string grant_type { get; set; } = string.Empty;
        public string client_id { get; set; } = string.Empty;
        public string client_secret { get; set; } = string.Empty;
        public object scope { get; set; } = string.Empty;
    }
}
