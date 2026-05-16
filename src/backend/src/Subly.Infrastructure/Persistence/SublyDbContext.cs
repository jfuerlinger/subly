using Microsoft.EntityFrameworkCore;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence;

public sealed class SublyDbContext(DbContextOptions<SublyDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SublyDbContext).Assembly);
    }
}
