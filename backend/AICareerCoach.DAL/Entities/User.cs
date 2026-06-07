using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.DAL.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string CareerGoal { get; set; } = string.Empty;

        public ICollection<CV> CVs { get; set; } = new List<CV>();

        public ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

        public ICollection<mockInterview> Interviews { get; set; } = new List<mockInterview>();
    }
}
