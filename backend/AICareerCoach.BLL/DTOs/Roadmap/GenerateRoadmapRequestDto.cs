using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class GenerateRoadmapRequestDto
    {
        [Required] public string TargetRole { get; set; } = string.Empty;
        public string? TemplateTrack { get; set; }
        public bool ForceRegenerate { get; set; }
    }
}
