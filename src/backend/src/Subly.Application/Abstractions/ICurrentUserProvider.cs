namespace Subly.Application.Abstractions;

public interface ICurrentUserProvider
{
    Guid GetRequiredUserId();
}
