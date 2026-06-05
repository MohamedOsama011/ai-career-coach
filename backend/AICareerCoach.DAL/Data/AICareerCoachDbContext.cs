using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.Data
{
    public class AICareerCoachDbContext : DbContext
    {
        public AICareerCoachDbContext(
            DbContextOptions<AICareerCoachDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }

        public DbSet<Roadmap> Roadmaps { get; set; }

        public DbSet<Interview> Interviews { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<CV> CVs { get; set; }

       
    }
}