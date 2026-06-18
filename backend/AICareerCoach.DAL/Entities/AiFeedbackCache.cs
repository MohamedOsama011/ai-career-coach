using AICareerCoach.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class AiFeedbackCache
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string CvHash { get; set; } = string.Empty;   
        public string FeedbackJson { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
