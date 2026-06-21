namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class SkillGapItemDto
    {
        public string SkillName { get; set; } = string.Empty;
        public string CurrentLevel { get; set; } = string.Empty;
        public string RequiredLevel { get; set; } = string.Empty;
        public string Gap { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }
}
