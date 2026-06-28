namespace AICareerCoach.BLL.DTOs.Job
{
    public class JobExplanationDto
    {
        public string Explanation { get; set; } = string.Empty;
        public List<string> MissingSkills { get; set; } = new();
    }
}
