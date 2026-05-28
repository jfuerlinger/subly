namespace Subly.Application.Contracts;

public sealed record LogoSuggestionDto(
    string Provider,
    string Domain,
    string LogoUrl);
