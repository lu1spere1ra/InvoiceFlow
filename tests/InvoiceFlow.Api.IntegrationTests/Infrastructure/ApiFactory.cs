using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceFlow.Api.IntegrationTests.Infrastructure;

/// <summary>Boots the real API against a fresh database with reference and demo data seeded.</summary>
public sealed class ApiFactory(SqlServerContainerFixture sqlServer) : WebApplicationFactory<Program>
{
    private readonly string _database = $"InvoiceFlow_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", sqlServer.ConnectionStringFor(_database));
        builder.UseSetting("Database:InitializeOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Signing:KeyPath", Path.Combine(Path.GetTempPath(), "invoiceflow-tests", $"{_database}.pem"));
        builder.UseSetting("Signing:GenerateIfMissing", "true");
    }
}
