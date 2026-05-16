using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence.Configurations;

internal sealed class CategoryEntityConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
