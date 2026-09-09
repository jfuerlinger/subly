using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class NotificationSettingsEntityConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("NotificationSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.LeadDays).IsRequired();
        builder.Property(x => x.Channels).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
