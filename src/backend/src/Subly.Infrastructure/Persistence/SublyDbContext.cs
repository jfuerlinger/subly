using Microsoft.EntityFrameworkCore;
using Subly.Domain.Models;

namespace Subly.Infrastructure.Persistence;

public sealed class SublyDbContext(DbContextOptions<SublyDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SublyDbContext).Assembly);
    }
}
