using System.Globalization;
using System.Text;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Invoices;

/// <summary>
/// Content of the QR code AT requires on invoices (Portaria 195/2020): <c>key:value</c> pairs joined by
/// <c>*</c>. Only mainland VAT (fiscal space "PT") is supported.
/// </summary>
public static class AtQrCode
{
    /// <returns>The payload, or <c>null</c> for drafts (no number or ATCUD yet).</returns>
    public static string? Build(InvoiceDto invoice, CompanyProfile company)
    {
        if (invoice.DocumentNumber is null || invoice.Atcud is null || invoice.IssueDate is null)
        {
            return null;
        }

        var fields = new List<(string Key, string Value)>
        {
            ("A", company.Nif),
            ("B", invoice.Customer.Nif),
            ("C", invoice.Customer.Country),
            ("D", invoice.Type.ToCode()),
            ("E", invoice.Status == InvoiceStatus.Voided ? "A" : "N"),
            ("F", invoice.IssueDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)),
            ("G", invoice.DocumentNumber),
            ("H", invoice.Atcud),
            ("I1", "PT"),
        };

        AddRate(fields, invoice, TaxRateCode.Exempt, baseKey: "I2", taxKey: null);
        AddRate(fields, invoice, TaxRateCode.Reduced, "I3", "I4");
        AddRate(fields, invoice, TaxRateCode.Intermediate, "I5", "I6");
        AddRate(fields, invoice, TaxRateCode.Normal, "I7", "I8");

        fields.Add(("N", Amount(invoice.TaxTotal)));
        fields.Add(("O", Amount(invoice.GrossTotal)));
        fields.Add(("Q", invoice.HashExcerpt ?? "0"));
        fields.Add(("R", company.SoftwareCertificateNumber));

        var payload = new StringBuilder();
        foreach (var (key, value) in fields)
        {
            payload.Append(payload.Length == 0 ? "" : "*").Append(key).Append(':').Append(value);
        }

        return payload.ToString();
    }

    private static void AddRate(List<(string, string)> fields, InvoiceDto invoice, TaxRateCode code, string baseKey, string? taxKey)
    {
        var rows = invoice.TaxSummary.Where(t => t.Code == code).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        fields.Add((baseKey, Amount(rows.Sum(r => r.TaxableAmount))));
        if (taxKey is not null)
        {
            fields.Add((taxKey, Amount(rows.Sum(r => r.TaxAmount))));
        }
    }

    private static string Amount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
