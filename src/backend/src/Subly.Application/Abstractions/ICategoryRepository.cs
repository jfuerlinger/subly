using Subly.Domain.Models;

namespace Subly.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> HasSubscriptionsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task ReassignSubscriptionsAsync(Guid sourceCategoryId, Guid targetCategoryId, CancellationToken cancellationToken = default);

    void Remove(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
