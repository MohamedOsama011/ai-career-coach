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

            }
        }
        public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
        {
            public void Configure(EntityTypeBuilder<Subscription> builder)
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).HasMaxLength(30).IsRequired();
                builder.Property(u => u.Price).IsRequired();

                builder.HasMany(u => u.Subscriptions).WithOne(c => c.Subscription)
                    .HasForeignKey(c => c.Subscriptionid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();


            }
        }



        public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
        {
            public void Configure(EntityTypeBuilder<Payment> builder)
            {

                builder.HasKey(x => x.Id);
                builder.Property(x => x.Amount).IsRequired();
                builder.Property(u => u.Status).HasDefaultValue("pending");
                builder.Property(p => p.invoicenumber).IsRequired(false);
                builder.Property(p => p.PaymentMethod).IsRequired(false);
                builder.Property(p => p.intentkey).IsRequired(false);
                //builder.Property(p => p.InvoiceKey).IsRequired(false);
                //builder.Property(p => p.referenceNumber).IsRequired(false);
                builder.Property(p => p.transactionid).IsRequired(false);
                builder.Property(p => p.transactionkey).IsRequired(false);





                builder.HasOne(u => u.UserSubscription).WithMany(c => c.Payments)
                    .HasForeignKey(c => c.Usersubscriptionid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();


            }
        }
        public class usersubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
        {
            public void Configure(EntityTypeBuilder<UserSubscription> builder)
            {

                builder.HasKey(x => x.Id);
                builder.Property(x => x.StartDate).IsRequired(false);
                builder.Property(u => u.Status).HasDefaultValue("notactice");
                builder.Property(p => p.Quantity).HasDefaultValue(1);
                builder.Property(p => p.Isactive).HasDefaultValue(false);
                builder.Property(p => p.Enddate).IsRequired(false);
                




                builder.HasOne(u => u.User).WithMany(c => c.UserSubscriptions)
                    .HasForeignKey(c => c.Userid)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                builder.HasOne(u=>u.Subscription).WithMany(s=>s.Subscriptions).HasForeignKey(u=>u.Subscriptionid).OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();


            }
        }
    }
}
