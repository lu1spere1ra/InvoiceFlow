using System.Net;
using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class DocumentLifecycleApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Invoice_receipt_is_paid_on_issue()
    {
        var receipt = await IssuedAsync(DocumentType.InvoiceReceipt);

        receipt.Status.ShouldBe(InvoiceStatus.Paid);
        receipt.DocumentNumber!.ShouldStartWith("FR ");
        receipt.PaidOn.ShouldBe(receipt.IssueDate);
    }

    [Fact]
    public async Task Invoice_can_be_paid()
    {
        var invoice = await IssuedAsync();

        var response = await Client.PostAsJsonAsync($"/api/invoices/{invoice.Id}/pay", new MarkPaidInput(null), Json, Ct);

        var paid = await ReadAsync<InvoiceDto>(response);
        paid.Status.ShouldBe(InvoiceStatus.Paid);
        paid.PaidOn.ShouldBe(invoice.IssueDate);
    }

    [Fact]
    public async Task Voided_invoice_keeps_its_number()
    {
        var invoice = await IssuedAsync();

        var response = await Client.PostAsJsonAsync($"/api/invoices/{invoice.Id}/void", new VoidInput("Wrong customer"), Json, Ct);

        var voided = await ReadAsync<InvoiceDto>(response);
        voided.Status.ShouldBe(InvoiceStatus.Voided);
        voided.DocumentNumber.ShouldBe(invoice.DocumentNumber);
        voided.VoidReason.ShouldBe("Wrong customer");
    }

    [Fact]
    public async Task Credit_notes_reduce_what_remains_creditable_and_block_voiding()
    {
        var invoice = await IssuedAsync(quantity: 10); // 10 × 65 € + 23% = 799.50 €

        var note = await CreateCreditNoteAsync(invoice.Id, quantity: 4);
        note.Type.ShouldBe(DocumentType.CreditNote);
        note.OriginalDocumentNumber.ShouldBe(invoice.DocumentNumber);
        note.GrossTotal.ShouldBe(319.80m);

        var issuedNote = await ReadAsync<InvoiceDto>(await Client.PostAsync($"/api/invoices/{note.Id}/issue", null, Ct));
        issuedNote.DocumentNumber!.ShouldStartWith("NC ");

        var original = await GetAsync<InvoiceDto>($"/api/invoices/{invoice.Id}");
        original.CreditedAmount.ShouldBe(319.80m);

        // 6 more units fit exactly; 7 would exceed what is left.
        var tooMuch = await Client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id}/credit-notes", new CreateCreditNoteInput("x", [new CreditNoteLineInput(1, 7)]), Json, Ct);
        tooMuch.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadAsync<ProblemDetails>(tooMuch)).Extensions["code"]!.ToString().ShouldBe("Invoice.CreditExceedsRemaining");

        var voiding = await Client.PostAsJsonAsync($"/api/invoices/{invoice.Id}/void", new VoidInput("x"), Json, Ct);
        voiding.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Credit_note_drafts_cannot_be_edited()
    {
        var invoice = await IssuedAsync();
        var note = await CreateCreditNoteAsync(invoice.Id, quantity: 1);

        var response = await Client.PutAsJsonAsync(
            $"/api/invoices/{note.Id}", new UpdateInvoiceDraftInput(note.CustomerId, 0, []), Json, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Lists_filter_overdue_and_sort_by_total()
    {
        await IssuedAsync(quantity: 1);
        await IssuedAsync(quantity: 3);

        var byTotal = await GetAsync<PagedResult<InvoiceSummaryDto>>("/api/invoices?status=Issued&sortBy=Total&descending=false&pageSize=100");
        byTotal.Items.Select(i => i.GrossTotal).ShouldBeInOrder(SortDirection.Ascending);

        // Nothing issued today is overdue yet.
        var overdue = await GetAsync<PagedResult<InvoiceSummaryDto>>("/api/invoices?overdueOnly=true");
        overdue.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Issued_documents_are_protected_at_the_persistence_level()
    {
        var invoice = await IssuedAsync();

        var attempt = await Should.ThrowAsync<InvalidOperationException>(() => WithDbAsync(async db =>
        {
            var tracked = await db.Invoices.SingleAsync(i => i.Id == invoice.Id, Ct);
            db.Entry(tracked).Property(i => i.GrossTotal).CurrentValue = 1m; // bypassing the aggregate on purpose
            return await db.SaveChangesAsync(Ct);
        }));

        attempt.Message.ShouldContain("immutable");
    }

    [Fact]
    public async Task Changes_are_audited_with_the_signed_in_user()
    {
        var invoice = await IssuedAsync();

        var audit = await WithDbAsync(db => db.Invoices
            .Where(i => i.Id == invoice.Id)
            .Select(i => new
            {
                CreatedBy = EF.Property<string>(i, "CreatedBy"),
                UpdatedBy = EF.Property<string>(i, "UpdatedBy"),
                UpdatedAt = EF.Property<DateTimeOffset?>(i, "UpdatedAt"),
            })
            .SingleAsync(Ct));

        audit.CreatedBy.ShouldBe("admin@invoiceflow.local");
        audit.UpdatedBy.ShouldBe("admin@invoiceflow.local");
        audit.UpdatedAt.ShouldNotBeNull();
    }

    private async Task<InvoiceDto> CreateCreditNoteAsync(Guid invoiceId, decimal quantity)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/invoices/{invoiceId}/credit-notes",
            new CreateCreditNoteInput("Returned hours", [new CreditNoteLineInput(1, quantity)]),
            Json,
            Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await ReadAsync<InvoiceDto>(response);
    }
}
