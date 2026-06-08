using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.Data
{
    public class AICareerCoachDbContext : IdentityDbContext<User>
    {
        public AICareerCoachDbContext(
            DbContextOptions<AICareerCoachDbContext> options)
            : base(options)
        {
        }

        public DbSet<Roadmap> Roadmaps { get; set; }

        public DbSet<RoadmapStep> RoadmapSteps { get; set; }

        public DbSet<mockInterview> Interviews { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<CV> CVs { get; set; }
    }
}
