using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(InvoiceFlow.Api.IntegrationTests.Infrastructure.SqlServerContainerFixture))]

namespace InvoiceFlow.Api.IntegrationTests.Infrastructure;

/// <summary>One real SQL Server container for the whole test run; each test class gets its own database.</summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionStringFor(string database) =>
        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = database }
            .ConnectionString;

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
