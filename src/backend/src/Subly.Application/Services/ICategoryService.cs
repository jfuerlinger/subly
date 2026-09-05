using Subly.Application.Contracts;

namespace Subly.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<CategoryDto> RenameCategoryAsync(Guid id, string newName, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(Guid id, Guid? replacementCategoryId, CancellationToken cancellationToken = default);
}
