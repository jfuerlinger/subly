using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subly.Application.Abstractions;
using Subly.Infrastructure.Persistence;

namespace Subly.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"subly-tests-{Guid.NewGuid():N}";

    public TestEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var dbName = _dbName;
            services.Replace(ServiceDescriptor.Scoped<SublyDbContext>(_ =>
            {
                var opts = new DbContextOptionsBuilder<SublyDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;
                return new SublyDbContext(opts);
            }));
            services.Replace(ServiceDescriptor.Singleton<IEmailSender>(EmailSender));
        });
    }
}

public sealed class TestEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string BodyHtml)> SentMessages { get; } = [];

    public Task SendAsync(string toEmail, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((toEmail, subject, bodyHtml));
        return Task.CompletedTask;
    }
}
