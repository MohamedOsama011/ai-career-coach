using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICareerCoach.DAL.Configurations
{
    public  class Configuration
    {
        public class UserConfiguration:IEntityTypeConfiguration<User>
        {
            public void Configure(EntityTypeBuilder<User>builder)
            {
                //properties
                builder.HasKey(x => x.Id);
                builder.Property(x=>x.FullName).HasMaxLength(100).IsRequired();
                builder.Property(u=>u.Email).IsRequired();

                //relations
                builder.HasMany(u => u.CVs).WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                builder.HasMany(u=>u.MockInterviews).WithOne(c => c.User)
                    .HasForeignKey(i=>i.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                    

            }
        }
    }
}
