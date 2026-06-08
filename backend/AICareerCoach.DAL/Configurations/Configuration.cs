using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICareerCoach.DAL.Configurations
{
    public class Configuration
    {
        public class UserConfiguration : IEntityTypeConfiguration<User>
        {
            public void Configure(EntityTypeBuilder<User> builder)
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FullName).HasMaxLength(100).IsRequired();
                builder.Property(u => u.Email).IsRequired();

                builder.HasMany(u => u.CVs).WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                builder.HasMany(u => u.Interviews).WithOne(c => c.User)
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            }
        }
    }
}
