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

            ConfigureInterviewEntities(builder);
        }

        /// <summary>
        /// Registers the normalized interview model (Session → Messages,
        /// Session 1:0..1 Scorecard). Session→User is unidirectional
        /// (no collection navigation on <see cref="User"/>) like Job→JobEmbedding.
        /// Enums stored as readable strings (cf. RoadmapStep.Level).
        /// </summary>
        private static void ConfigureInterviewEntities(ModelBuilder builder)
        {
            builder.Entity<InterviewSession>(e =>
            {
                e.Property(s => s.UserId).HasMaxLength(450);
                e.Property(s => s.TargetRole).HasMaxLength(256);
                e.Property(s => s.SummaryContextJson).HasMaxLength(-1);

                e.Property(s => s.Track).HasConversion<string>().HasMaxLength(32);
                e.Property(s => s.Difficulty).HasConversion<string>().HasMaxLength(32);
                e.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);

                e.Property(s => s.RowVersion).IsRowVersion();

                e.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => s.UserId);
                e.HasIndex(s => new { s.UserId, s.Status });
            });

            builder.Entity<InterviewMessage>(e =>
            {
                e.Property(m => m.Role).HasConversion<string>().HasMaxLength(32);

                e.HasOne(m => m.Session)
                    .WithMany(s => s.Messages)
                    .HasForeignKey(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(m => m.SessionId);
                e.HasIndex(m => new { m.SessionId, m.TurnNumber });
            });

            builder.Entity<InterviewScorecard>(e =>
            {
                e.Property(c => c.LetterGrade).HasMaxLength(8);
                e.Property(c => c.OverallSummary).HasMaxLength(-1);
                e.Property(c => c.StrengthsJson).HasMaxLength(-1);
                e.Property(c => c.ImprovementsJson).HasMaxLength(-1);
                e.Property(c => c.QuestionAnalysisJson).HasMaxLength(-1);

                e.HasOne(c => c.Session)
                    .WithOne(s => s.Scorecard)
                    .HasForeignKey<InterviewScorecard>(c => c.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(c => c.SessionId).IsUnique();
            });
        }

        public DbSet<Roadmap> Roadmaps { get; set; }

        public DbSet<RoadmapStep> RoadmapSteps { get; set; }

        public DbSet<InterviewSession> InterviewSessions { get; set; }

        public DbSet<InterviewMessage> InterviewMessages { get; set; }

        public DbSet<InterviewScorecard> InterviewScorecards { get; set; }

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
