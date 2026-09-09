using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subly.Infrastructure.Services;

namespace Subly.Api.Tests;

public sealed class EmailOptionsBindingTests
{
    [Fact]
    public void EmailOptions_ShouldBindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = "smtp.example.com",
                ["Email:Port"] = "2525",
                ["Email:Username"] = "user@example.com",
                ["Email:Password"] = "secret",
                ["Email:FromAddress"] = "noreply@subly.local",
                ["Email:FromName"] = "Subly Reminders",
                ["Email:UseSsl"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<EmailOptions>>().Value;

        options.Host.Should().Be("smtp.example.com");
        options.Port.Should().Be(2525);
        options.Username.Should().Be("user@example.com");
        options.FromAddress.Should().Be("noreply@subly.local");
        options.FromName.Should().Be("Subly Reminders");
        options.UseSsl.Should().BeFalse();
    }
}
