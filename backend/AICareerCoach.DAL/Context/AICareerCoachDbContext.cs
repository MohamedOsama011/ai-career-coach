using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.Data
{
    public class AICareerCoachDbContext : DbContext
    {
        //send configuration of our context to main context(Dbcontext)  
        public AICareerCoachDbContext(DbContextOptions<AICareerCoachDbContext> options): base(options)
        {}
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(AICareerCoachDbContext).Assembly);
            base.OnModelCreating(builder);
        }
        public DbSet<User> Users { get; set; }

        public DbSet<Roadmap> Roadmaps { get; set; }

        public DbSet<mockInterview> Interviews { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<CV> CVs { get; set; }

       
    }
}