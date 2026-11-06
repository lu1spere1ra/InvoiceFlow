using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Api.IntegrationTests.Infrastructure;

/// <summary>Base class: one API instance and one database per test class.</summary>
public abstract class IntegrationTest : IClassFixture<ApiFactoryFixture>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApiFactoryFixture _fixture;

    protected IntegrationTest(ApiFactoryFixture fixture)
    {
        _fixture = fixture;
        Client = fixture.AdminClient;
    }

    /// <summary>Signed in as the demo admin.</summary>
    protected HttpClient Client { get; }

    /// <summary>Direct database access for assertions on things the API does not expose (audit columns).</summary>
    protected async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected HttpClient AnonymousClient() => _fixture.Factory.CreateClient();

    protected Task<HttpClient> ClientForAsync(string email, string password) => _fixture.SignedInClientAsync(email, password);

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected async Task<T> GetAsync<T>(string url) =>
        (await Client.GetFromJsonAsync<T>(url, Json, Ct))!;

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;

    protected async Task<ProductDto> AnyProductAsync(string code = "CONS-H") =>
        (await GetAsync<List<ProductDto>>("/api/products")).Single(p => p.Code == code);

    protected async Task<InvoiceDto> IssuedAsync(
        DocumentType type = DocumentType.Invoice, string productCode = "CONS-H", decimal quantity = 1)
    {
        var customer = await SeededCustomerAsync();
        var product = await AnyProductAsync(productCode);
        var draft = await ReadAsync<InvoiceDto>(await Client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceDraftInput(type, customer.Id, 30, [new InvoiceLineInput(product.Id, quantity)]),
            Json,
            Ct));
        var issued = await Client.PostAsync($"/api/invoices/{draft.Id}/issue", null, Ct);
        issued.EnsureSuccessStatusCode();
        return await ReadAsync<InvoiceDto>(issued);
    }

    protected async Task<CustomerDto> SeededCustomerAsync(string nif = "509123457") =>
        (await GetAsync<PagedResult<CustomerDto>>($"/api/customers?search={nif}")).Items.Single();
}

/// <summary>xUnit class fixture wrapper so the factory can receive the assembly-wide container.</summary>
public sealed class ApiFactoryFixture(SqlServerContainerFixture sqlServer) : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new(sqlServer);

    public HttpClient AdminClient { get; private set; } = null!;

    public async ValueTask InitializeAsync() =>
        // Creating the server runs migrations and seeding once for the class.
        AdminClient = await SignedInClientAsync("admin@invoiceflow.local", "Admin!2026");

    public async Task<HttpClient> SignedInClientAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
        return client;
    }

    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}
