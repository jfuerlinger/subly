using Subly.Application.Abstractions;
using Subly.Application.Contracts;
using Subly.Domain.Models;

namespace Subly.Application.Services;

public sealed class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await repository.ListAsync(cancellationToken);
        return categories
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToArray();
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        var trimmedName = name.Trim();

        var existing = await repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Category '{trimmedName}' already exists.", nameof(name));

        var category = Category.Create(trimmedName);
        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    public async Task<CategoryDto> RenameCategoryAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name is required.", nameof(newName));

        var category = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Category '{id}' not found.");

        var trimmedName = newName.Trim();

        var existing = await repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing is not null && existing.Id != id)
            throw new ArgumentException($"Category '{trimmedName}' already exists.", nameof(newName));

        category.Rename(trimmedName);
        await repository.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }

    public async Task DeleteCategoryAsync(Guid id, Guid? replacementCategoryId, CancellationToken cancellationToken = default)
    {
        var category = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Category '{id}' not found.");

        var hasSubscriptions = await repository.HasSubscriptionsAsync(id, cancellationToken);
        if (hasSubscriptions)
        {
            if (!replacementCategoryId.HasValue || replacementCategoryId.Value == id)
            {
                throw new ArgumentException("A different replacement category is required for a category with subscriptions.", nameof(replacementCategoryId));
            }

            var replacementCategory = await repository.GetByIdAsync(replacementCategoryId.Value, cancellationToken)
                ?? throw new ArgumentException("The replacement category does not exist.", nameof(replacementCategoryId));

            await repository.ReassignSubscriptionsAsync(category.Id, replacementCategory.Id, cancellationToken);
        }

        repository.Remove(category);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
