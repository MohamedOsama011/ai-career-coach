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

        public string Email { get; set; } = string.Empty;

        public string CareerGoal { get; set; } = string.Empty;

        public ICollection<Roadmap> Roadmaps { get; set; }
            = new List<Roadmap>();

        public ICollection<Interview> Interviews { get; set; }
            = new List<Interview>();
    }
}
