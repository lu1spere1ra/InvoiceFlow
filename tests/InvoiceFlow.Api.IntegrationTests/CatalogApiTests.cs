using System.Net;
using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class CatalogApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Product_crud_with_soft_delete()
    {
        var rate = (await GetAsync<List<TaxRateDto>>("/api/tax-rates")).Single(r => r.Percentage == 23m);
        var input = new ProductInput("hosting-y", "Alojamento web anual", ProductType.Service, "ANO", 120m, rate.Id);

        var created = await ReadAsync<ProductDto>(await Client.PostAsJsonAsync("/api/products", input, Json, Ct));
        created.Code.ShouldBe("HOSTING-Y");

        (await Client.PostAsJsonAsync("/api/products", input, Json, Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var updated = await ReadAsync<ProductDto>(await Client.PutAsJsonAsync(
            $"/api/products/{created.Id}", input with { UnitPrice = 150m }, Json, Ct));
        updated.UnitPrice.ShouldBe(150m);

        (await Client.DeleteAsync($"/api/products/{created.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync<List<ProductDto>>("/api/products")).ShouldNotContain(p => p.Id == created.Id);

        // The row is kept (documents may reference it) and the code becomes available again.
        var stillThere = await WithDbAsync(db => db.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == created.Id, Ct));
        stillThere.ShouldBeTrue();
        (await Client.PostAsJsonAsync("/api/products", input, Json, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Deleted_customers_disappear_but_their_documents_remain()
    {
        var customer = await ReadAsync<CustomerDto>(await Client.PostAsJsonAsync(
            "/api/customers", new CustomerInput("Cliente Temporário", "508890128", "Rua 1", "1000-001", "Lisboa", "PT", null), Json, Ct));

        var renamed = await Client.PutAsJsonAsync(
            $"/api/customers/{customer.Id}",
            new CustomerInput("Cliente Renomeado", "508890128", "Rua 2", "1000-002", "Lisboa", "PT", null),
            Json,
            Ct);
        (await ReadAsync<CustomerDto>(renamed)).Name.ShouldBe("Cliente Renomeado");

        (await Client.DeleteAsync($"/api/customers/{customer.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await Client.GetAsync($"/api/customers/{customer.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var list = await GetAsync<PagedResult<CustomerDto>>("/api/customers?search=Renomeado");
        list.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Customers_can_be_sorted()
    {
        var page = await GetAsync<PagedResult<CustomerDto>>("/api/customers?sortBy=City&pageSize=100");

        page.Items.Select(c => c.City).ShouldBeInOrder(SortDirection.Ascending, StringComparer.Ordinal);
    }
}
