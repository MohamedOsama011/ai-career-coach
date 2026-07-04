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

        /// <summary>
        /// Auto-touches <see cref="InterviewSession.UpdatedAt"/> on any Modified
        /// InterviewSession entry so status transitions are always timestamped
        /// even if a call site forgets to set it (Phase 6, M4). Added entries
        /// rely on the entity's field initializer.
        /// </summary>
        public override int SaveChanges()
        {
            TouchUpdatedAt();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TouchUpdatedAt();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TouchUpdatedAt()
        {
            foreach (var entry in ChangeTracker.Entries<InterviewSession>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
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

            builder.Entity<Job>()
                .HasIndex(j => new { j.ExternalId, j.Source })
                .IsUnique()
                .HasFilter("[ExternalId] IS NOT NULL");

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
            ConfigureChatEntities(builder);
            ConfigurePaymentEntities(builder);

            builder.Entity<JobSyncLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ErrorMessages).HasMaxLength(-1);
                e.HasIndex(x => x.SyncedAt);
            });
        }

        private static void ConfigurePaymentEntities(ModelBuilder builder)
        {
            builder.Entity<Subscription>(e =>
            {
                e.Property(s => s.Name).HasMaxLength(256);

                e.HasMany(s => s.UserSubscriptions)
                    .WithOne(us => us.Subscription)
                    .HasForeignKey(us => us.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserSubscription>(e =>
            {
                e.Property(us => us.UserId).HasMaxLength(450);
                e.Property(us => us.Status).HasMaxLength(32);

                e.HasOne(us => us.User)
                    .WithMany(u => u.UserSubscriptions)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(us => us.Subscription)
                    .WithMany(s => s.UserSubscriptions)
                    .HasForeignKey(us => us.SubscriptionId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(us => us.UserId);
            });

            builder.Entity<Payment>(e =>
            {
                e.Property(p => p.Status).HasMaxLength(32);
                e.Property(p => p.IntentKey).HasMaxLength(256);
                e.Property(p => p.InvoiceNumber).HasMaxLength(256);
                e.Property(p => p.TransactionId).HasMaxLength(256);
                e.Property(p => p.TransactionKey).HasMaxLength(256);
                e.Property(p => p.PaymentMethod).HasMaxLength(128);

                e.HasOne(p => p.UserSubscription)
                    .WithMany(us => us.Payments)
                    .HasForeignKey(p => p.UserSubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(p => p.IntentKey);
                e.HasIndex(p => p.UserSubscriptionId);
            });
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

        /// <summary>
        /// Registers the chat-assistant conversation model (Session → Messages).
        /// Session→User is unidirectional (no collection navigation on
        /// <see cref="User"/>) like InterviewSession. Enums stored as
        /// readable strings (cf. InterviewMessage.Role). <see cref="ChatSession.UpdatedAt"/>
        /// is NOT auto-touched here — the service sets it manually per the
        /// locked decision to avoid generalizing the override.
        /// </summary>
        private static void ConfigureChatEntities(ModelBuilder builder)
        {
            builder.Entity<ChatSession>(e =>
            {
                e.Property(s => s.UserId).HasMaxLength(450);
                e.Property(s => s.Title).HasMaxLength(256);

                e.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => s.UserId);
                e.HasIndex(s => new { s.UserId, s.UpdatedAt });
            });

            builder.Entity<ChatMessage>(e =>
            {
                e.Property(m => m.Role).HasConversion<string>().HasMaxLength(32);
                e.Property(m => m.Content).HasMaxLength(-1);
                e.Property(m => m.ToolCallsJson).HasMaxLength(-1);
                e.Property(m => m.ToolCallId).HasMaxLength(64);
                e.Property(m => m.ToolName).HasMaxLength(64);

                e.HasOne(m => m.Session)
                    .WithMany(s => s.Messages)
                    .HasForeignKey(m => m.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(m => m.SessionId);
                e.HasIndex(m => new { m.SessionId, m.OrderIndex });
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
        public DbSet<JobSyncLog> JobSyncLogs { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
    }
}
