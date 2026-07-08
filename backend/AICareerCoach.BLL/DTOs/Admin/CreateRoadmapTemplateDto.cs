using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class CreateRoadmapTemplateDto
    {
        [Required] public string Track { get; set; } = string.Empty;
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<AdminCreateRoadmapStepDto> Steps { get; set; } = new();
    }

    public class UpdateRoadmapTemplateDto
    {
        [Required] public string Track { get; set; } = string.Empty;
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<AdminCreateRoadmapStepDto> Steps { get; set; } = new();
    }

    public class AdminCreateRoadmapStepDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public string Level { get; set; } = string.Empty;
        public List<string> Resources { get; set; } = new();
        [Required] public int OrderIndex { get; set; }
    }

    public class TestMatchRequestDto
    {
        public string? SampleCvText { get; set; }
    }

    public class TestMatchResultDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
