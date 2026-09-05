using Microsoft.EntityFrameworkCore;
using Subly.Application.Abstractions;
using Subly.Domain.Models;
using Subly.Infrastructure.Persistence;

namespace Subly.Infrastructure.Persistence.Repositories;

internal sealed class EfCategoryRepository(SublyDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await dbContext.Categories.AddAsync(category, cancellationToken);
    }

    public Task<bool> HasSubscriptionsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return dbContext.Subscriptions.AnyAsync(s => s.CategoryId == categoryId, cancellationToken);
    }

    public async Task ReassignSubscriptionsAsync(Guid sourceCategoryId, Guid targetCategoryId, CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.Subscriptions
            .Where(s => s.CategoryId == sourceCategoryId)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            subscription.ReassignCategory(targetCategoryId);
        }
    }

    public void Remove(Category category)
    {
        dbContext.Categories.Remove(category);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
