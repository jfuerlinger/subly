using System.Text.Json;

namespace Subly.Cli.Services;

internal static class JsonSerializerOptionsProvider
{
    internal static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
