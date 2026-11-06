using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class SaftExportTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    private static readonly XNamespace Ns = "urn:OECD:StandardAuditFile-Tax:PT_1.04_01";

    static SaftExportTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Fact]
    public async Task Export_is_valid_against_the_official_xsd_and_has_consistent_totals()
    {
        var invoice = await IssuedAsync(quantity: 10);
        await IssuedAsync(DocumentType.InvoiceReceipt, "LIVRO-01", 2);
        await CreateDraftOnlyAsync();                                           // drafts are never exported
        var voided = await IssuedAsync(quantity: 1);
        await Client.PostAsJsonAsync($"/api/invoices/{voided.Id}/void", new VoidInput("Duplicated"), Json, Ct);
        var note = await ReadAsync<InvoiceDto>(await Client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id}/credit-notes", new CreateCreditNoteInput("Hours not used", [new CreditNoteLineInput(1, 2)]), Json, Ct));
        await Client.PostAsync($"/api/invoices/{note.Id}/issue", null, Ct); // 2 h × 65 € = 130.00 net, a debit

        var (xml, raw) = await ExportAsync(invoice.IssueDate!.Value);

        ValidateAgainstXsd(raw).ShouldBeEmpty();

        // Other tests in this class share the database, so totals are checked for internal consistency.
        var sales = xml.Root!.Element(Ns + "SourceDocuments")!.Element(Ns + "SalesInvoices")!;
        var entries = sales.Elements(Ns + "Invoice").ToList();
        var effective = entries.Where(i => i.Element(Ns + "DocumentStatus")!.Element(Ns + "InvoiceStatus")!.Value != "A").ToList();
        decimal NetOf(XElement e) => Amount(e.Element(Ns + "DocumentTotals")!.Element(Ns + "NetTotal")!);

        int.Parse(sales.Element(Ns + "NumberOfEntries")!.Value, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(entries.Count);
        Amount(sales.Element(Ns + "TotalCredit")!).ShouldBe(effective.Where(e => e.Element(Ns + "InvoiceType")!.Value != "NC").Sum(NetOf));
        Amount(sales.Element(Ns + "TotalDebit")!).ShouldBe(effective.Where(e => e.Element(Ns + "InvoiceType")!.Value == "NC").Sum(NetOf));
        Amount(sales.Element(Ns + "TotalDebit")!).ShouldBeGreaterThanOrEqualTo(130.00m);
        entries.ShouldNotContain(e => e.Element(Ns + "InvoiceNo")!.Value.Length == 0);

        var voidedEntry = sales.Elements(Ns + "Invoice").Single(i => i.Element(Ns + "InvoiceNo")!.Value == voided.DocumentNumber);
        voidedEntry.Element(Ns + "DocumentStatus")!.Element(Ns + "InvoiceStatus")!.Value.ShouldBe("A");

        var noteEntry = sales.Elements(Ns + "Invoice").Single(i => i.Element(Ns + "InvoiceType")!.Value == "NC");
        noteEntry.Descendants(Ns + "Reference").Single().Value.ShouldBe(invoice.DocumentNumber);
        noteEntry.Descendants(Ns + "DebitAmount").ShouldNotBeEmpty();

        xml.Root.Element(Ns + "Header")!.Element(Ns + "TaxAccountingBasis")!.Value.ShouldBe("F");
        xml.Declaration!.Encoding.ShouldBe("windows-1252", StringCompareShould.IgnoreCase);
    }

    [Fact]
    public async Task Schema_validation_really_runs()
    {
        var invoice = await IssuedAsync();
        var (xml, _) = await ExportAsync(invoice.IssueDate!.Value);

        // Remove a mandatory element: the validator must notice, otherwise the test above proves nothing.
        xml.Descendants(Ns + "ATCUD").First().Remove();
        using var corrupted = new MemoryStream();
        xml.Save(corrupted);

        ValidateAgainstXsd(corrupted.ToArray()).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Period_must_be_within_one_fiscal_year() =>
        (await Client.GetAsync("/api/saft?from=2025-12-01&to=2026-01-31", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    [Fact]
    public async Task Viewers_cannot_export()
    {
        var viewer = await ClientForAsync("viewer@invoiceflow.local", "Viewer!2026");

        (await viewer.GetAsync("/api/saft?from=2026-01-01&to=2026-12-31", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static decimal Amount(XElement element) =>
        decimal.Parse(element.Value, System.Globalization.CultureInfo.InvariantCulture);

    private async Task CreateDraftOnlyAsync()
    {
        var customer = await SeededCustomerAsync();
        var product = await AnyProductAsync();
        (await Client.PostAsJsonAsync(
            "/api/invoices",
            new CreateInvoiceDraftInput(DocumentType.Invoice, customer.Id, 30, [new InvoiceLineInput(product.Id, 1)]),
            Json,
            Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private async Task<(XDocument Xml, byte[] Raw)> ExportAsync(DateOnly day)
    {
        var response = await Client.GetAsync($"/api/saft?from={day.Year}-01-01&to={day.Year}-12-31", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsByteArrayAsync(Ct);
        using var stream = new MemoryStream(raw);
        return (XDocument.Load(stream), raw);
    }

    private static List<string> ValidateAgainstXsd(byte[] raw)
    {
        var schemas = new XmlSchemaSet();
        using (var xsd = XmlReader.Create(Path.Combine(AppContext.BaseDirectory, "Saft", "saftpt1.04_01.xsd")))
        {
            schemas.Add(null, xsd);
        }

        var errors = new List<string>();
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = schemas };
        settings.ValidationEventHandler += (_, e) => errors.Add($"{e.Severity}: {e.Message} (line {e.Exception?.LineNumber})");
        using var reader = XmlReader.Create(new MemoryStream(raw), settings);
        while (reader.Read())
        {
        }

        return errors;
    }
}
