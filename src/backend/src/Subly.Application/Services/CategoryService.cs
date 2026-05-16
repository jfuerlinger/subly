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
}
