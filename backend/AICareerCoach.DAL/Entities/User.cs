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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Roadmap> Roadmaps { get; set; } = new HashSet<Roadmap>();
        public ICollection<Interview> Interviews { get; set; } = new HashSet<Interview>();
        public ICollection<CV> CVs { get; set; } = new HashSet<CV>();
    }
}
