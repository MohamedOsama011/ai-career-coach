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

        public ICollection<CV> CVs { get; set; } = new HashSet<CV>();

        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new HashSet<RefreshToken>();
        public virtual ICollection<UserSubscription>? UserSubscriptions { get; set; }= new HashSet<UserSubscription>();
    }
}
