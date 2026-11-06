using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Saft;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Saft;

/// <summary>
/// SAF-T (PT) 1.04_01 writer for invoicing software (TaxAccountingBasis "F"): Header, MasterFiles and
/// SourceDocuments/SalesInvoices. Written element by element with <see cref="XmlWriter"/>, in the order
/// the official XSD demands; an integration test validates the output against that XSD.
/// </summary>
internal sealed class SaftPtExporter(AppDbContext db, CompanyProfile company, TimeProvider timeProvider) : ISaftExporter
{
    private const string Namespace = "urn:OECD:StandardAuditFile-Tax:PT_1.04_01";
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss";
    private static readonly TimeZoneInfo Lisbon = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");

    static SaftPtExporter() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public async Task<byte[]> ExportAsync(SaftPeriod period, CancellationToken cancellationToken)
    {
        var documents = await db.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Draft && i.IssueDate >= period.From && i.IssueDate <= period.To)
            .OrderBy(i => i.Type).ThenBy(i => i.SeriesId).ThenBy(i => i.Number)
            .Select(i => new SourceDocument(i, EF.Property<string>(i, ShadowProperties.CreatedBy), EF.Property<DateTimeOffset?>(i, ShadowProperties.UpdatedAt)))
            .ToListAsync(cancellationToken);

        var productIds = documents.SelectMany(d => d.Invoice.Lines).Select(l => l.ProductId).Distinct().ToList();
        var productTypes = await db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Type, cancellationToken);

        // AT requires Windows-1252 for SAF-T files.
        using var buffer = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = Encoding.GetEncoding(1252), Indent = true, Async = true };
        await using (var xml = XmlWriter.Create(buffer, settings))
        {
            await xml.WriteStartDocumentAsync();
            xml.WriteStartElement("AuditFile", Namespace);
            WriteHeader(xml, period);
            WriteMasterFiles(xml, documents, productTypes);
            WriteSalesInvoices(xml, documents);
            xml.WriteEndElement();
            await xml.WriteEndDocumentAsync();
        }

        return buffer.ToArray();
    }

    private void WriteHeader(XmlWriter xml, SaftPeriod period)
    {
        xml.WriteStartElement("Header");
        Element(xml, "AuditFileVersion", "1.04_01");
        Element(xml, "CompanyID", company.Nif);
        Element(xml, "TaxRegistrationNumber", company.Nif);
        Element(xml, "TaxAccountingBasis", "F");
        Element(xml, "CompanyName", Max(company.Name, 100));
        xml.WriteStartElement("CompanyAddress");
        Element(xml, "AddressDetail", Max(company.Street, 210));
        Element(xml, "City", Max(company.City, 50));
        Element(xml, "PostalCode", company.PostalCode);
        Element(xml, "Country", company.Country);
        xml.WriteEndElement();
        Element(xml, "FiscalYear", period.From.Year.ToString(CultureInfo.InvariantCulture));
        Element(xml, "StartDate", Date(period.From));
        Element(xml, "EndDate", Date(period.To));
        Element(xml, "CurrencyCode", "EUR");
        Element(xml, "DateCreated", Date(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Lisbon).DateTime)));
        Element(xml, "TaxEntity", "Global");
        Element(xml, "ProductCompanyTaxID", company.Nif);
        Element(xml, "SoftwareCertificateNumber", company.SoftwareCertificateNumber);
        Element(xml, "ProductID", "InvoiceFlow/InvoiceFlow portfolio project");
        Element(xml, "ProductVersion", typeof(SaftPtExporter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "1.0.0");
        Element(xml, "HeaderComment", "Not certified software. Demonstration data only.");
        if (company.Email is not null)
        {
            Element(xml, "Email", company.Email);
        }

        xml.WriteEndElement();
    }

    private static void WriteMasterFiles(XmlWriter xml, List<SourceDocument> documents, Dictionary<Guid, ProductType> productTypes)
    {
        xml.WriteStartElement("MasterFiles");

        // Customers come from the documents' own snapshots, so the file shows exactly what was invoiced,
        // even for customers edited or deleted since. One entry per NIF, using the latest document.
        foreach (var customer in documents.Select(d => d.Invoice).OrderBy(i => i.SystemEntryDate)
                     .GroupBy(i => i.Customer.Nif).Select(g => g.Last().Customer))
        {
            xml.WriteStartElement("Customer");
            Element(xml, "CustomerID", customer.Nif);
            Element(xml, "AccountID", "Desconhecido");
            Element(xml, "CustomerTaxID", customer.Nif);
            Element(xml, "CompanyName", Max(customer.Name, 100));
            xml.WriteStartElement("BillingAddress");
            Element(xml, "AddressDetail", Max(customer.Address.Street, 210));
            Element(xml, "City", Max(customer.Address.City, 50));
            Element(xml, "PostalCode", Max(customer.Address.PostalCode, 20));
            Element(xml, "Country", customer.Address.Country);
            xml.WriteEndElement();
            Element(xml, "SelfBillingIndicator", "0");
            xml.WriteEndElement();
        }

        foreach (var line in documents.SelectMany(d => d.Invoice.Lines).GroupBy(l => l.ProductCode).Select(g => g.First()))
        {
            xml.WriteStartElement("Product");
            Element(xml, "ProductType", productTypes.GetValueOrDefault(line.ProductId) == ProductType.Goods ? "P" : "S");
            Element(xml, "ProductCode", line.ProductCode);
            Element(xml, "ProductDescription", Max(line.Description, 200));
            Element(xml, "ProductNumberCode", line.ProductCode);
            xml.WriteEndElement();
        }

        var taxes = documents.SelectMany(d => d.Invoice.Lines)
            .Select(l => (l.TaxCode, l.TaxPercentage)).Distinct().OrderByDescending(t => t.TaxPercentage).ToList();
        if (taxes.Count > 0)
        {
            xml.WriteStartElement("TaxTable");
            foreach (var (code, percentage) in taxes)
            {
                xml.WriteStartElement("TaxTableEntry");
                Element(xml, "TaxType", "IVA");
                Element(xml, "TaxCountryRegion", "PT");
                Element(xml, "TaxCode", code.ToSaftCode());
                Element(xml, "Description", TaxDescription(code));
                Element(xml, "TaxPercentage", Percentage(percentage));
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    private static void WriteSalesInvoices(XmlWriter xml, List<SourceDocument> documents)
    {
        // Totals exclude voided documents; credit notes are debits, invoices are credits.
        var effective = documents.Where(d => d.Invoice.Status != InvoiceStatus.Voided).Select(d => d.Invoice).ToList();

        xml.WriteStartElement("SourceDocuments");
        xml.WriteStartElement("SalesInvoices");
        Element(xml, "NumberOfEntries", documents.Count.ToString(CultureInfo.InvariantCulture));
        Element(xml, "TotalDebit", Money(effective.Where(i => i.Type == DocumentType.CreditNote).Sum(i => i.NetTotal)));
        Element(xml, "TotalCredit", Money(effective.Where(i => i.Type != DocumentType.CreditNote).Sum(i => i.NetTotal)));

        foreach (var document in documents)
        {
            WriteInvoice(xml, document);
        }

        xml.WriteEndElement();
        xml.WriteEndElement();
    }

    private static void WriteInvoice(XmlWriter xml, SourceDocument document)
    {
        var invoice = document.Invoice;
        var sourceId = Max(document.CreatedBy ?? "system", 30);
        var isCreditNote = invoice.Type == DocumentType.CreditNote;

        xml.WriteStartElement("Invoice");
        Element(xml, "InvoiceNo", invoice.DocumentNumber!);
        Element(xml, "ATCUD", invoice.Atcud!);

        xml.WriteStartElement("DocumentStatus");
        if (invoice.Status == InvoiceStatus.Voided)
        {
            Element(xml, "InvoiceStatus", "A");
            Element(xml, "InvoiceStatusDate", LocalDateTime(invoice.VoidedAt ?? document.UpdatedAt ?? invoice.SystemEntryDate!.Value));
            Element(xml, "Reason", Max(invoice.VoidReason ?? "", 50));
        }
        else
        {
            Element(xml, "InvoiceStatus", "N");
            Element(xml, "InvoiceStatusDate", LocalDateTime(invoice.SystemEntryDate!.Value));
        }

        Element(xml, "SourceID", sourceId);
        Element(xml, "SourceBilling", "P");
        xml.WriteEndElement();

        Element(xml, "Hash", invoice.Hash ?? "0");
        Element(xml, "HashControl", invoice.HashControl ?? "0");
        Element(xml, "Period", invoice.IssueDate!.Value.Month.ToString(CultureInfo.InvariantCulture));
        Element(xml, "InvoiceDate", Date(invoice.IssueDate.Value));
        Element(xml, "InvoiceType", invoice.Type.ToCode());
        xml.WriteStartElement("SpecialRegimes");
        Element(xml, "SelfBillingIndicator", "0");
        Element(xml, "CashVATSchemeIndicator", "0");
        Element(xml, "ThirdPartiesBillingIndicator", "0");
        xml.WriteEndElement();
        Element(xml, "SourceID", sourceId);
        Element(xml, "SystemEntryDate", LocalDateTime(invoice.SystemEntryDate!.Value));
        Element(xml, "CustomerID", invoice.Customer.Nif);

        foreach (var line in invoice.Lines)
        {
            xml.WriteStartElement("Line");
            Element(xml, "LineNumber", line.LineNumber.ToString(CultureInfo.InvariantCulture));
            Element(xml, "ProductCode", line.ProductCode);
            Element(xml, "ProductDescription", Max(line.Description, 200));
            Element(xml, "Quantity", line.Quantity.ToString("0.######", CultureInfo.InvariantCulture));
            Element(xml, "UnitOfMeasure", line.Unit);
            // SAF-T unit price is net of line discounts.
            Element(xml, "UnitPrice", (line.UnitPrice * (1 - (line.DiscountPercent / 100m))).ToString("0.######", CultureInfo.InvariantCulture));
            Element(xml, "TaxPointDate", Date(invoice.IssueDate.Value));
            if (isCreditNote)
            {
                xml.WriteStartElement("References");
                Element(xml, "Reference", invoice.OriginalDocumentNumber ?? "");
                Element(xml, "Reason", Max(invoice.CreditReason ?? "", 50));
                xml.WriteEndElement();
            }

            Element(xml, "Description", Max(line.Description, 200));
            Element(xml, isCreditNote ? "DebitAmount" : "CreditAmount", Money(line.NetAmount));
            xml.WriteStartElement("Tax");
            Element(xml, "TaxType", "IVA");
            Element(xml, "TaxCountryRegion", "PT");
            Element(xml, "TaxCode", line.TaxCode.ToSaftCode());
            Element(xml, "TaxPercentage", Percentage(line.TaxPercentage));
            xml.WriteEndElement();
            if (line.ExemptionCode is not null)
            {
                Element(xml, "TaxExemptionReason", Max(line.ExemptionReason ?? "", 60));
                Element(xml, "TaxExemptionCode", line.ExemptionCode);
            }

            xml.WriteEndElement();
        }

        xml.WriteStartElement("DocumentTotals");
        Element(xml, "TaxPayable", Money(invoice.TaxTotal));
        Element(xml, "NetTotal", Money(invoice.NetTotal));
        Element(xml, "GrossTotal", Money(invoice.GrossTotal));
        if (invoice.Type == DocumentType.InvoiceReceipt && invoice.PaidOn is { } paidOn)
        {
            xml.WriteStartElement("Payment");
            Element(xml, "PaymentMechanism", "OU");
            Element(xml, "PaymentAmount", Money(invoice.GrossTotal));
            Element(xml, "PaymentDate", Date(paidOn));
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
        xml.WriteEndElement();
    }

    private static void Element(XmlWriter xml, string name, string value) => xml.WriteElementString(name, value);

    private static string Max(string value, int length) => value.Length <= length ? value : value[..length];

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string LocalDateTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Lisbon).ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Percentage(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string TaxDescription(TaxRateCode code) => code switch
    {
        TaxRateCode.Normal => "Taxa normal",
        TaxRateCode.Intermediate => "Taxa intermédia",
        TaxRateCode.Reduced => "Taxa reduzida",
        _ => "Isento",
    };

    private sealed record SourceDocument(Invoice Invoice, string? CreatedBy, DateTimeOffset? UpdatedAt);
}
