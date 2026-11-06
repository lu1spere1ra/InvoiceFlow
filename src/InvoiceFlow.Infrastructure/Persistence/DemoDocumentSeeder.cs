using InvoiceFlow.Application.Series;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Infrastructure.Persistence;

/// <summary>
/// Creates a year of realistic activity through the domain model itself, so every seeded document is
/// numbered, signed into the hash chain and valid in SAF-T: invoices (mostly paid, some overdue),
/// invoice-receipts, a few credit notes and one voided document. Deterministic (fixed random seed).
/// </summary>
internal sealed class DemoDocumentSeeder(AppDbContext db, IAtSeriesRegistry registry, IDocumentSigner signer)
{
    private static readonly TimeZoneInfo Lisbon = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
    private readonly Random _random = new(2026);
    private readonly Dictionary<(DocumentType, int), DocumentSeries> _series = [];

    public async Task SeedAsync(
        IReadOnlyList<Customer> customers, IReadOnlyList<Product> products, IReadOnlyDictionary<Guid, TaxRate> rates,
        DateOnly today, CancellationToken cancellationToken)
    {
        var companies = customers.Where(c => !c.Nif.IsFinalConsumer).ToList();
        var finalConsumer = customers.Single(c => c.Nif.IsFinalConsumer);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var issued = new List<Invoice>();

        for (var month = start; month <= today; month = month.AddMonths(1))
        {
            // Business grows a little over the year.
            var documents = 4 + _random.Next(3) + ((month.Month - start.Month + 12) % 12 / 3);
            var days = Enumerable.Range(0, documents)
                .Select(_ => month.AddDays(_random.Next(DateTime.DaysInMonth(month.Year, month.Month))))
                .Where(day => day <= today)
                .Order()
                .ToList();

            foreach (var day in days)
            {
                var isReceipt = _random.NextDouble() < 0.25;
                var customer = isReceipt && _random.NextDouble() < 0.5 ? finalConsumer : companies[_random.Next(companies.Count)];
                var invoice = Invoice.CreateDraft(isReceipt ? DocumentType.InvoiceReceipt : DocumentType.Invoice, customer, 30).Value;
                AddLines(invoice, products, rates, smallTicket: customer.Nif.IsFinalConsumer);

                await IssueAsync(invoice, day, cancellationToken);
                issued.Add(invoice);

                // Most invoices older than their due date were paid, some 1–25 days late; recent ones stay open.
                if (invoice.Type == DocumentType.Invoice && _random.NextDouble() < (day < today.AddDays(-45) ? 0.85 : 0.35))
                {
                    var paidOn = invoice.DueDate!.Value.AddDays(_random.Next(-20, 25));
                    invoice.MarkAsPaid(paidOn < day ? day : paidOn > today ? today : paidOn);
                }
            }
        }

        await AddCorrectionsAsync(issued, today, cancellationToken);

        foreach (var series in _series.Values.Where(s => s.Code[..4] != today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)))
        {
            series.Close();
        }

        db.Invoices.AddRange(issued);
    }

    private void AddLines(Invoice invoice, IReadOnlyList<Product> products, IReadOnlyDictionary<Guid, TaxRate> rates, bool smallTicket)
    {
        var candidates = smallTicket ? products.Where(p => p.UnitPrice < 100).ToList() : products;
        foreach (var product in candidates.OrderBy(_ => _random.Next()).Take(1 + _random.Next(smallTicket ? 2 : 3)))
        {
            var rate = rates[product.TaxRateId];
            var quantity = product.Unit switch
            {
                "H" => 2 + _random.Next(smallTicket ? 3 : 30),
                "MES" => 1,
                _ => 1 + _random.Next(smallTicket ? 2 : 4),
            };
            var discount = !smallTicket && _random.NextDouble() < 0.2 ? 5m : 0m;
            invoice.AddLine(product, rate, quantity, product.UnitPrice, discount, rate.IsExempt ? "M07" : null);
        }
    }

    private async Task AddCorrectionsAsync(List<Invoice> issued, DateOnly today, CancellationToken cancellationToken)
    {
        // A partial credit note on three older invoices.
        foreach (var original in issued.Where(i => i.Type == DocumentType.Invoice && i.IssueDate < today.AddDays(-60)).Take(3).ToList())
        {
            var line = original.Lines[0];
            var quantity = Math.Max(1, Math.Floor(line.Quantity / 2));
            var note = Invoice.CreateCreditNote(original, "Hours invoiced twice", [new CreditNoteLine(line.LineNumber, quantity)], 0).Value;
            await IssueAsync(note, Max(original.IssueDate!.Value.AddDays(7), LastIssueDate(DocumentType.CreditNote)), cancellationToken);
            issued.Add(note);
        }

        // One invoice-receipt voided the same day it was issued.
        var mistake = issued.LastOrDefault(i => i.Type == DocumentType.InvoiceReceipt && i.IssueDate < today.AddDays(-10));
        mistake?.Void("Issued to the wrong customer", new DateTimeOffset(mistake.IssueDate!.Value.ToDateTime(new TimeOnly(17, 45)), TimeSpan.Zero));
    }

    private async Task IssueAsync(Invoice invoice, DateOnly day, CancellationToken cancellationToken)
    {
        var series = await SeriesForAsync(invoice.Type, day.Year, cancellationToken);
        var local = day.ToDateTime(new TimeOnly(9 + _random.Next(9), _random.Next(60), _random.Next(60)));
        var systemEntryDate = new DateTimeOffset(local, Lisbon.GetUtcOffset(local));
        var result = invoice.Issue(series, day, systemEntryDate, signer);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Demo data could not be issued: {result.Error.Message}");
        }
    }

    private async Task<DocumentSeries> SeriesForAsync(DocumentType type, int year, CancellationToken cancellationToken)
    {
        if (!_series.TryGetValue((type, year), out var series))
        {
            var code = $"{year}A";
            series = db.DocumentSeries.Local.SingleOrDefault(s => s.DocumentType == type && s.Code == code)
                ?? DocumentSeries.Create(type, code, await registry.RegisterAsync(type, code, cancellationToken)).Value;
            if (db.Entry(series).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                db.DocumentSeries.Add(series);
            }

            _series[(type, year)] = series;
        }

        return series;
    }

    private DateOnly LastIssueDate(DocumentType type) =>
        _series.Where(s => s.Key.Item1 == type).Select(s => s.Value.LastIssueDate ?? DateOnly.MinValue).DefaultIfEmpty(DateOnly.MinValue).Max();

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
}
