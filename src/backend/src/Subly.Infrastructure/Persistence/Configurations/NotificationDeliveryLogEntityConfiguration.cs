using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class NotificationDeliveryLogEntityConfiguration : IEntityTypeConfiguration<NotificationDeliveryLog>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryLog> builder)
    {
        builder.ToTable("NotificationDeliveryLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionId).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ForPaymentDate).IsRequired();
        builder.Property(x => x.SentAtUtc).IsRequired();

        builder.HasIndex(x => new { x.SubscriptionId, x.Channel, x.ForPaymentDate }).IsUnique();

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
