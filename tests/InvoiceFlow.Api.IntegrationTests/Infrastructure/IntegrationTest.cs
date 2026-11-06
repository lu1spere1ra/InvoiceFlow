using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;

namespace InvoiceFlow.Api.IntegrationTests.Infrastructure;

/// <summary>Base class: one API instance and one database per test class.</summary>
public abstract class IntegrationTest : IClassFixture<ApiFactoryFixture>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected IntegrationTest(ApiFactoryFixture fixture) => Client = fixture.Factory.CreateClient();

    protected HttpClient Client { get; }

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected async Task<T> GetAsync<T>(string url) =>
        (await Client.GetFromJsonAsync<T>(url, Json, Ct))!;

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;

    protected async Task<ProductDto> AnyProductAsync(string code = "CONS-H") =>
        (await GetAsync<List<ProductDto>>("/api/products")).Single(p => p.Code == code);

    protected async Task<CustomerDto> SeededCustomerAsync(string nif = "509123457") =>
        (await GetAsync<PagedResult<CustomerDto>>($"/api/customers?search={nif}")).Items.Single();
}

/// <summary>xUnit class fixture wrapper so the factory can receive the assembly-wide container.</summary>
public sealed class ApiFactoryFixture(SqlServerContainerFixture sqlServer) : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new(sqlServer);

    public ValueTask InitializeAsync()
    {
        // Creating the server runs migrations and seeding once for the class.
        _ = Factory.Server;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}
