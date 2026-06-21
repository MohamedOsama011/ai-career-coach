using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICareerCoach.DAL.Data
{
    public class AICareerCoachDbContext : IdentityDbContext<User>
    {
        public AICareerCoachDbContext(
            DbContextOptions<AICareerCoachDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<JobEmbedding>()
                .Property(e => e.Embedding)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>()
                );

            builder.Entity<RoadmapTemplateEmbedding>()
                .Property(e => e.Embedding)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>()
                );

            builder.Entity<RoadmapTemplateEmbedding>()
                .HasIndex(e => e.RoadmapId)
                .IsUnique();

            builder.Entity<UserRoadmap>(entity =>
            {
                entity.Property(r => r.UserId).HasMaxLength(450);
                entity.Property(r => r.CvHash).HasMaxLength(64);
                entity.Property(r => r.TargetRole).HasMaxLength(256);
                entity.Property(r => r.TemplateTrack).HasMaxLength(128);
                entity.HasIndex(r => new { r.UserId, r.CreatedAt });
            });
        }

        public DbSet<Roadmap> Roadmaps { get; set; }

        public DbSet<RoadmapStep> RoadmapSteps { get; set; }

        public DbSet<mockInterview> Interviews { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<CV> CVs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<AiFeedbackCache> AiFeedbackCaches { get; set; }

        public DbSet<JobEmbedding> JobEmbeddings { get; set; }
        public DbSet<JobRecommendationCache> JobRecommendationCaches { get; set; }
        public DbSet<UserRoadmap> UserRoadmaps { get; set; }
        public DbSet<RoadmapTemplateEmbedding> RoadmapTemplateEmbeddings { get; set; }
    }
}
