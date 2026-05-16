namespace Subly.Application.Abstractions;

public interface IDateProvider
{
    DateOnly Today { get; }
}
