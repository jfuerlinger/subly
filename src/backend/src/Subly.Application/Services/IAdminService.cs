namespace Subly.Application.Services;

public interface IAdminService
{
    Task DeleteAllDataAsync(CancellationToken cancellationToken = default);

    Task SeedDataAsync(CancellationToken cancellationToken = default);
}
