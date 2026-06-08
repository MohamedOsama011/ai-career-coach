using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class CreateRoadmapStepDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public string Level { get; set; } = string.Empty;
        public List<string> Resources { get; set; } = new();
        [Required] public int OrderIndex { get; set; }
    }
}
