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
        public string Id { get; set; }
        public string Cvid { get; set; }
        public string FeedbackJson { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual CV CV { get; set; }
    }
}
