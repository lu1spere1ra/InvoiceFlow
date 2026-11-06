using System.Net;
using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class InvoicesApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Draft_to_issued_end_to_end()
    {
        var draft = await CreateDraftAsync(quantity: 10);
        draft.Status.ShouldBe(InvoiceStatus.Draft);
        draft.DocumentNumber.ShouldBeNull();
        draft.NetTotal.ShouldBe(650m); // 10 h × 65 €
        draft.TaxTotal.ShouldBe(149.50m);
        draft.GrossTotal.ShouldBe(799.50m);

        var response = await Client.PostAsync($"/api/invoices/{draft.Id}/issue", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var issued = await ReadAsync<InvoiceDto>(response);
        issued.Status.ShouldBe(InvoiceStatus.Issued);
        issued.DocumentNumber!.ShouldMatch(@"^FT \d{4}A/\d+$");
        issued.Atcud!.ShouldMatch(@"^[A-Z0-9]{8}-\d+$");
        issued.IssueDate.ShouldNotBeNull();

        var fetched = await GetAsync<InvoiceDto>($"/api/invoices/{draft.Id}");
        fetched.DocumentNumber.ShouldBe(issued.DocumentNumber);
        fetched.Lines.ShouldHaveSingleItem().ProductCode.ShouldBe("CONS-H");
    }

    [Fact]
    public async Task Issued_documents_cannot_be_changed_issued_again_or_deleted()
    {
        var draft = await CreateDraftAsync();
        await Client.PostAsync($"/api/invoices/{draft.Id}/issue", null, Ct);

        var update = await Client.PutAsJsonAsync(
            $"/api/invoices/{draft.Id}", new UpdateInvoiceDraftInput(draft.CustomerId, 30, []), Json, Ct);
        var reissue = await Client.PostAsync($"/api/invoices/{draft.Id}/issue", null, Ct);
        var delete = await Client.DeleteAsync($"/api/invoices/{draft.Id}", Ct);

        update.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        reissue.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        delete.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadAsync<ProblemDetails>(reissue)).Extensions["code"]!.ToString().ShouldBe("Invoice.NotDraft");
    }

    [Fact]
    public async Task Drafts_can_be_updated_and_deleted()
    {
        var draft = await CreateDraftAsync();
        var product = await AnyProductAsync("LIVRO-01");

        var update = await Client.PutAsJsonAsync(
            $"/api/invoices/{draft.Id}",
            new UpdateInvoiceDraftInput(draft.CustomerId, 15, [new InvoiceLineInput(product.Id, 2)]),
            Json,
            Ct);

        var updated = await ReadAsync<InvoiceDto>(update);
        updated.PaymentTermDays.ShouldBe(15);
        updated.GrossTotal.ShouldBe(73.99m); // 69.80 + 6% = 4.188 → 4.19

        (await Client.DeleteAsync($"/api/invoices/{draft.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/invoices/{draft.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Exempt_product_without_reason_is_rejected()
    {
        var customer = await SeededCustomerAsync();
        var training = await AnyProductAsync("FORM-H");

        var response = await Client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceDraftInput(DocumentType.Invoice, customer.Id, 30, [new InvoiceLineInput(training.Id, 1)]),
            Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadAsync<ProblemDetails>(response)).Extensions["code"]!.ToString().ShouldBe("Invoice.ExemptionReasonRequired");
    }

    [Fact]
    public async Task Concurrent_issues_never_produce_duplicate_or_skipped_numbers()
    {
        var drafts = new List<InvoiceDto>();
        for (var i = 0; i < 8; i++)
        {
            drafts.Add(await CreateDraftAsync());
        }

        var responses = await Task.WhenAll(drafts.Select(d => Client.PostAsync($"/api/invoices/{d.Id}/issue", null, Ct)));

        // Losers of the race get a retryable 409; nobody gets a duplicate.
        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.Conflict);

        var issued = await GetAsync<PagedResult<InvoiceSummaryDto>>("/api/invoices?status=Issued&type=Invoice&pageSize=100");
        var numbers = issued.Items.Select(i => int.Parse(i.DocumentNumber!.Split('/')[1], System.Globalization.CultureInfo.InvariantCulture)).Order().ToList();
        numbers.ShouldBe(Enumerable.Range(1, numbers.Count).ToList());
    }

    [Fact]
    public async Task Lists_documents_filtered_by_status()
    {
        await CreateDraftAsync();

        var drafts = await GetAsync<PagedResult<InvoiceSummaryDto>>("/api/invoices?status=Draft");

        drafts.Items.ShouldNotBeEmpty();
        drafts.Items.ShouldAllBe(i => i.Status == InvoiceStatus.Draft);
    }

    private async Task<InvoiceDto> CreateDraftAsync(decimal quantity = 1)
    {
        var customer = await SeededCustomerAsync();
        var product = await AnyProductAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceDraftInput(DocumentType.Invoice, customer.Id, 30, [new InvoiceLineInput(product.Id, quantity)]),
            Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await ReadAsync<InvoiceDto>(response);
    }
}
