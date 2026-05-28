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

        var normalized = name.Trim().ToLowerInvariant();

        var existing = await repository.GetByNameAsync(normalized, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Category '{normalized}' already exists.", nameof(name));

        var category = Category.Create(normalized);
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

        var normalized = newName.Trim().ToLowerInvariant();

        var existing = await repository.GetByNameAsync(normalized, cancellationToken);
        if (existing is not null && existing.Id != id)
            throw new ArgumentException($"Category '{normalized}' already exists.", nameof(newName));

        category.Rename(normalized);
        await repository.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name);
    }
}
