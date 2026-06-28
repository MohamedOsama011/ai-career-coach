using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Interview
{
    public class StartSessionRequestDto
    {
        [Required]
        public string Track { get; set; } = string.Empty;

        [Required]
        public string Difficulty { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string TargetRole { get; set; } = string.Empty;
    }
}
