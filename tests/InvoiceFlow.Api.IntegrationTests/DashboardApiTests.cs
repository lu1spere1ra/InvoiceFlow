using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Application.Dashboard;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Api.IntegrationTests;

/// <summary>
/// The class database is shared, so each test measures the change its own documents cause.
/// </summary>
public sealed class DashboardApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Revenue_counts_invoices_minus_credit_notes_and_ignores_drafts_and_voided()
    {
        var before = await DashboardAsync();

        var invoice = await IssuedAsync(quantity: 10);                         // +650.00 net
        var voided = await IssuedAsync(quantity: 3);                           // voided: no effect
        await Client.PostAsJsonAsync($"/api/invoices/{voided.Id}/void", new VoidInput("x"), Json, Ct);
        var note = await ReadAsync<InvoiceDto>(await Client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id}/credit-notes", new CreateCreditNoteInput("x", [new CreditNoteLineInput(1, 4)]), Json, Ct));
        await Client.PostAsync($"/api/invoices/{note.Id}/issue", null, Ct);    // −260.00 net

        var after = await DashboardAsync();

        (after.RevenueThisMonth - before.RevenueThisMonth).ShouldBe(390.00m);
        (after.RevenueYearToDate - before.RevenueYearToDate).ShouldBe(390.00m);
        after.RevenueByMonth.Count.ShouldBe(12);
        after.RevenueByMonth[^1].NetRevenue.ShouldBe(after.RevenueThisMonth);
    }

    [Fact]
    public async Task Outstanding_is_net_of_credit_notes_and_cleared_by_payment()
    {
        var before = await DashboardAsync();

        var invoice = await IssuedAsync(quantity: 2);                          // 159.90 gross, due in 30 days
        var afterIssue = await DashboardAsync();
        (afterIssue.OutstandingTotal - before.OutstandingTotal).ShouldBe(159.90m);
        afterIssue.Outstanding.Single(b => b.Label == "Not yet due").Amount.ShouldBeGreaterThanOrEqualTo(159.90m);

        var note = await ReadAsync<InvoiceDto>(await Client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id}/credit-notes", new CreateCreditNoteInput("x", [new CreditNoteLineInput(1, 1)]), Json, Ct));
        await Client.PostAsync($"/api/invoices/{note.Id}/issue", null, Ct);    // −79.95
        (await DashboardAsync()).OutstandingTotal.ShouldBe(before.OutstandingTotal + 79.95m);

        await Client.PostAsJsonAsync($"/api/invoices/{invoice.Id}/pay", new MarkPaidInput(null), Json, Ct);
        (await DashboardAsync()).OutstandingTotal.ShouldBe(before.OutstandingTotal);
    }

    [Fact]
    public async Task Top_customers_rank_by_net_revenue()
    {
        var bigSpender = await ReadAsync<CustomerDto>(await Client.PostAsJsonAsync(
            "/api/customers", new CustomerInput("Grande Cliente, SA", "234567899", "Av. 1", "1000-001", "Lisboa", "PT", null), Json, Ct));
        var product = await AnyProductAsync("PORT-01");
        var draft = await ReadAsync<InvoiceDto>(await Client.PostAsJsonAsync(
            "/api/invoices", new CreateInvoiceDraftInput(DocumentType.Invoice, bigSpender.Id, 30, [new InvoiceLineInput(product.Id, 100)]), Json, Ct));
        await Client.PostAsync($"/api/invoices/{draft.Id}/issue", null, Ct);   // 89,900 net: more than any seed customer

        var top = (await DashboardAsync()).TopCustomers;

        top.Count.ShouldBeLessThanOrEqualTo(5);
        top[0].Nif.ShouldBe("234567899");
        top[0].NetRevenue.ShouldBe(89900m);
        top.Select(c => c.NetRevenue).ShouldBeInOrder(SortDirection.Descending);
    }

    private Task<DashboardDto> DashboardAsync() => GetAsync<DashboardDto>("/api/dashboard");
}
