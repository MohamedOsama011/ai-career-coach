using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        /// Auto-touches <see cref="InterviewSession.UpdatedAt"/>,
        /// <see cref="Payment.UpdatedAt"/>, <see cref="Subscription.UpdatedAt"/>,
        /// and <see cref="UserSubscription.UpdatedAt"/> on any Modified entry
        /// so status transitions are always timestamped even if a call site
        /// forgets to set it (interview: Phase 6, M4; payment: Phase 1).
        /// Added entries rely on the entity's field initializer.
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
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<InterviewSession>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }
            foreach (var entry in ChangeTracker.Entries<Payment>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }
            foreach (var entry in ChangeTracker.Entries<Subscription>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }
            foreach (var entry in ChangeTracker.Entries<UserSubscription>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
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
            builder.Entity<JobEmbedding>()
                .Property(e => e.Embedding)
                .Metadata.SetValueComparer(new ValueComparer<float[]>(
                    (a, b) => (a ?? Array.Empty<float>()).SequenceEqual(b ?? Array.Empty<float>()),
                    v => (v ?? Array.Empty<float>()).Aggregate(0, (hash, f) => HashCode.Combine(hash, f.GetHashCode())),
                    v => (v ?? Array.Empty<float>()).ToArray()
                ));

            builder.Entity<Job>(e =>
            {
                e.Property(j => j.Salary).HasPrecision(18, 2);
                e.HasIndex(j => new { j.ExternalId, j.Source })
                    .IsUnique()
                    .HasFilter("[ExternalId] IS NOT NULL");
            });

            builder.Entity<RoadmapTemplateEmbedding>()
                .Property(e => e.Embedding)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>()
                );
            builder.Entity<RoadmapTemplateEmbedding>()
                .Property(e => e.Embedding)
                .Metadata.SetValueComparer(new ValueComparer<float[]>(
                    (a, b) => (a ?? Array.Empty<float>()).SequenceEqual(b ?? Array.Empty<float>()),
                    v => (v ?? Array.Empty<float>()).Aggregate(0, (hash, f) => HashCode.Combine(hash, f.GetHashCode())),
                    v => (v ?? Array.Empty<float>()).ToArray()
                ));

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
            ConfigureSubscriptionAuditLogEntity(builder);

        builder.Entity<JobSyncLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ErrorMessages).HasMaxLength(-1);
            e.HasIndex(x => x.SyncedAt);
        });

        ConfigureAdminAuditLogEntity(builder);
        ConfigureNotificationEntity(builder);
        }

        private static void ConfigureNotificationEntity(ModelBuilder builder)
        {
            builder.Entity<Notification>(e =>
            {
                e.Property(n => n.UserId).HasMaxLength(450);
                e.Property(n => n.Title).HasMaxLength(256);
                e.Property(n => n.Body).HasMaxLength(-1);
                e.Property(n => n.Type).HasMaxLength(32);

                e.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(n => n.UserId);
                e.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            });
        }

        private static void ConfigureAdminAuditLogEntity(ModelBuilder builder)
        {
            builder.Entity<AdminAuditLog>(e =>
            {
                e.Property(a => a.Action).HasMaxLength(64);
                e.Property(a => a.TargetType).HasMaxLength(64);
                e.Property(a => a.TargetId).HasMaxLength(450);
                e.Property(a => a.Details).HasMaxLength(-1);

                e.HasOne(a => a.AdminUser)
                    .WithMany()
                    .HasForeignKey(a => a.AdminUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(a => a.AdminUserId);
                e.HasIndex(a => a.TargetType);
                e.HasIndex(a => a.Timestamp);
            });
        }

        private static void ConfigureSubscriptionAuditLogEntity(ModelBuilder builder)
        {
            builder.Entity<SubscriptionAuditLog>(e =>
            {
                e.Property(a => a.Action).HasMaxLength(64);
                e.Property(a => a.PreviousValues).HasMaxLength(-1);
                e.Property(a => a.NewValues).HasMaxLength(-1);
                e.Property(a => a.Notes).HasMaxLength(-1);

                e.HasOne(a => a.AdminUser)
                    .WithMany()
                    .HasForeignKey(a => a.AdminUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(a => a.UserSubscription)
                    .WithMany()
                    .HasForeignKey(a => a.UserSubscriptionId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(a => a.UserSubscriptionId);
                e.HasIndex(a => a.AdminUserId);
                e.HasIndex(a => a.CreatedAt);
            });
        }

        private static void ConfigurePaymentEntities(ModelBuilder builder)
        {
            builder.Entity<Subscription>(e =>
            {
                e.Property(s => s.Name).HasMaxLength(256);
                e.Property(s => s.Price).HasPrecision(18, 2);

                e.HasMany(s => s.UserSubscriptions)
                    .WithOne(us => us.Subscription)
                    .HasForeignKey(us => us.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserSubscription>(e =>
            {
                e.Property(us => us.UserId).HasMaxLength(450);

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
                e.Property(p => p.Amount).HasPrecision(18, 2);
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
        public DbSet<SubscriptionAuditLog> SubscriptionAuditLogs { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}
