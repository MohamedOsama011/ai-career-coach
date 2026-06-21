namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class SkillsCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public List<SkillGapItemDto> Skills { get; set; } = new();
    }
}
