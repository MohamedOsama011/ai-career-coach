using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class CreateRoadmapDto
    {
        [Required] public string Track { get; set; } = string.Empty;
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<CreateRoadmapStepDto> Steps { get; set; } = new();
    }
}
