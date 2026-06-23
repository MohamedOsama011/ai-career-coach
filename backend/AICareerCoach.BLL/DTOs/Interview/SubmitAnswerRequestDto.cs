using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Interview
{
    public class SubmitAnswerRequestDto
    {
        [Required, MinLength(1)]
        public string Answer { get; set; } = string.Empty;
    }
}
