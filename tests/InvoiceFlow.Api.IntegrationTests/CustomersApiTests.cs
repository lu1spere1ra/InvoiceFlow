using System.Net;
using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class CustomersApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    private static CustomerInput Input(string nif = "515789011") =>
        new("Livraria Lello Nova, Lda", nif, "Rua das Carmelitas 144", "4050-161", "Porto", "PT", "geral@lello.example");

    [Fact]
    public async Task Creates_and_reads_a_customer()
    {
        var response = await Client.PostAsJsonAsync("/api/customers", Input(), Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await ReadAsync<CustomerDto>(response);
        response.Headers.Location!.ToString().ShouldEndWith($"/api/customers/{created.Id}");

        var fetched = await GetAsync<CustomerDto>($"/api/customers/{created.Id}");
        fetched.ShouldBe(created);
    }

    [Fact]
    public async Task Returns_field_errors_for_malformed_input()
    {
        var response = await Client.PostAsJsonAsync("/api/customers", Input() with { Name = "", PostalCode = "4050" }, Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        problem.Errors.Keys.ShouldBe(["Name", "PostalCode"], ignoreOrder: true);
    }

    [Fact]
    public async Task Rejects_a_nif_with_a_wrong_check_digit_as_a_business_error()
    {
        var response = await Client.PostAsJsonAsync("/api/customers", Input(nif: "515789012"), Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadAsync<ProblemDetails>(response)).Extensions["code"]!.ToString().ShouldBe("Customer.InvalidNif");
    }

    [Fact]
    public async Task Rejects_a_duplicate_nif()
    {
        var response = await Client.PostAsJsonAsync("/api/customers", Input(nif: "509123457"), Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Lists_customers_with_paging_and_search()
    {
        var page = await GetAsync<PagedResult<CustomerDto>>("/api/customers?pageSize=3");
        page.Items.Count.ShouldBe(3);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(8);

        var search = await GetAsync<PagedResult<CustomerDto>>("/api/customers?search=Padaria");
        search.Items.ShouldHaveSingleItem().Name.ShouldBe("Padaria Central, Lda");
    }

    [Fact]
    public async Task Rejects_oversized_pages() =>
        (await Client.GetAsync("/api/customers?pageSize=1000", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    [Fact]
    public async Task Unknown_customer_is_404() =>
        (await Client.GetAsync($"/api/customers/{Guid.NewGuid()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
}
