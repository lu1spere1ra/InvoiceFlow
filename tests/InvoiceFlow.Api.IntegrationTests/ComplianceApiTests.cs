using System.Net;
using System.Net.Http.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Series;
using InvoiceFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class ComplianceApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Issued_documents_carry_a_hash_excerpt_and_atcud()
    {
        var invoice = await IssuedAsync();

        invoice.HashExcerpt.ShouldNotBeNull();
        invoice.HashExcerpt.Length.ShouldBe(4);
        invoice.Atcud.ShouldEndWith($"-{invoice.DocumentNumber!.Split('/')[1]}");
    }

    [Fact]
    public async Task Pdf_is_generated_for_issued_documents()
    {
        var invoice = await IssuedAsync();

        var response = await Client.GetAsync($"/api/invoices/{invoice.Id}/pdf", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync(Ct);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).ShouldBe("%PDF-");
        response.Content.Headers.ContentDisposition!.FileName.ShouldBe($"{invoice.DocumentNumber!.Replace(' ', '_').Replace('/', '-')}.pdf");
    }

    [Fact]
    public async Task Series_verification_detects_tampering_in_the_database()
    {
        var first = await IssuedAsync(DocumentType.InvoiceReceipt);
        var second = await IssuedAsync(DocumentType.InvoiceReceipt);
        await IssuedAsync(DocumentType.InvoiceReceipt);
        var series = (await GetAsync<List<SeriesDto>>("/api/series")).Single(s => s.IsActive && s.DocumentType == DocumentType.InvoiceReceipt);

        var before = await GetAsync<SeriesVerificationDto>($"/api/series/{series.Id}/verification");
        before.IsValid.ShouldBeTrue();
        before.DocumentsChecked.ShouldBeGreaterThanOrEqualTo(3);

        // Someone edits a total straight in SQL, bypassing the application and its guards.
        await WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE Invoices SET GrossTotal = GrossTotal - 10 WHERE Id = {second.Id}", Ct));

        var after = await GetAsync<SeriesVerificationDto>($"/api/series/{series.Id}/verification");
        after.IsValid.ShouldBeFalse();
        after.FirstBrokenNumber.ShouldBe(int.Parse(second.DocumentNumber!.Split('/')[1], System.Globalization.CultureInfo.InvariantCulture));
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public async Task Only_one_active_series_per_type_and_only_admins_manage_series()
    {
        var accountant = await ClientForAsync("accountant@invoiceflow.local", "Accountant!2026");
        (await accountant.PostAsJsonAsync("/api/series", new CreateSeriesInput(DocumentType.CreditNote, "2027A"), Json, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await Client.PostAsJsonAsync("/api/series", new CreateSeriesInput(DocumentType.CreditNote, "2027A"), Json, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var current = (await GetAsync<List<SeriesDto>>("/api/series")).Single(s => s.IsActive && s.DocumentType == DocumentType.CreditNote);
        (await Client.PostAsync($"/api/series/{current.Id}/close", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var created = await Client.PostAsJsonAsync("/api/series", new CreateSeriesInput(DocumentType.CreditNote, "2027A"), Json, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var series = await ReadAsync<SeriesDto>(created);
        series.ValidationCode.Length.ShouldBe(8);
        series.LastNumber.ShouldBe(0);
    }
}
