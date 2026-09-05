using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionEntityConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Vendor).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.PaymentMethod).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.Property(x => x.Cycle).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AutoRenew).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.LogoUrl).HasColumnType("text");
        builder.Property(x => x.NextPaymentDate).IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.NextPaymentDate);
        builder.HasIndex(x => x.CancelledAt);
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
