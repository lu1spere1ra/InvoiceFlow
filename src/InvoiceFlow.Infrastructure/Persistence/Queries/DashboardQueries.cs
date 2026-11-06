using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Dashboard;
using InvoiceFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

/// <summary>
/// Read-only aggregates computed in SQL (GROUP BY with a signed amount: credit notes count negative).
/// Ageing of open invoices is bucketed in memory because only unpaid invoices are loaded.
/// </summary>
internal sealed class DashboardQueries(AppDbContext db, TimeProvider timeProvider) : IDashboardQueries
{
    private static readonly (string Label, int Min, int? Max)[] Buckets =
    [
        ("Not yet due", int.MinValue, 0),
        ("1–30 days", 1, 30),
        ("31–60 days", 31, 60),
        ("61–90 days", 61, 90),
        ("Over 90 days", 91, null),
    ];

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = timeProvider.TodayInPortugal();
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        var effective = db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Paid);

        // Flatten first: EF Core translates GROUP BY over plain columns, not over complex-type members.
        var signed = effective.Select(i => new
        {
            i.IssueDate,
            CustomerNif = i.Customer.Nif,
            CustomerName = i.Customer.Name,
            Net = i.Type == DocumentType.CreditNote ? -i.NetTotal : i.NetTotal,
            Counted = i.Type == DocumentType.CreditNote ? 0 : 1,
        });

        var monthly = await signed
            .Where(i => i.IssueDate >= firstMonth)
            .GroupBy(i => new { i.IssueDate!.Value.Year, i.IssueDate.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Net = g.Sum(i => i.Net), Count = g.Sum(i => i.Counted) })
            .ToListAsync(cancellationToken);

        // Every month appears, including months without documents, so the chart has no holes.
        var revenueByMonth = Enumerable.Range(0, 12)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => monthly.FirstOrDefault(m => m.Year == month.Year && m.Month == month.Month) is { } m
                ? new MonthlyRevenueDto(month.Year, month.Month, m.Net, m.Count)
                : new MonthlyRevenueDto(month.Year, month.Month, 0, 0))
            .ToList();

        var yearToDate = await signed
            .Where(i => i.IssueDate >= new DateOnly(today.Year, 1, 1))
            .SumAsync(i => i.Net, cancellationToken);

        var topCustomers = (await signed
            .Where(i => i.IssueDate >= firstMonth)
            .GroupBy(i => i.CustomerNif)
            .Select(g => new { Nif = g.Key, Name = g.Max(i => i.CustomerName), Net = g.Sum(i => i.Net), Count = g.Sum(i => i.Counted) })
            .OrderByDescending(c => c.Net)
            .Take(5)
            .ToListAsync(cancellationToken))
            .Select(c => new TopCustomerDto(c.Nif, c.Name ?? "", c.Net, c.Count))
            .ToList();

        // Open invoices: issued, not paid, net of credit notes issued against them.
        var open = await db.Invoices.AsNoTracking()
            .Where(i => i.Type == DocumentType.Invoice && i.Status == InvoiceStatus.Issued)
            .Select(i => new
            {
                i.DueDate,
                Balance = i.GrossTotal - (db.Invoices
                    .Where(n => n.OriginalInvoiceId == i.Id && (n.Status == InvoiceStatus.Issued || n.Status == InvoiceStatus.Paid))
                    .Sum(n => (decimal?)n.GrossTotal) ?? 0),
            })
            .ToListAsync(cancellationToken);

        var balances = open.Where(o => o.Balance > 0)
            .Select(o => (o.Balance, DaysOverdue: o.DueDate is { } due ? today.DayNumber - due.DayNumber : 0))
            .ToList();

        var outstanding = Buckets
            .Select(b =>
            {
                var inBucket = balances.Where(x => x.DaysOverdue >= b.Min && (b.Max is null || x.DaysOverdue <= b.Max)).ToList();
                return new AgingBucketDto(b.Label, Math.Max(b.Min, 0), b.Max, inBucket.Sum(x => x.Balance), inBucket.Count);
            })
            .ToList();

        return new DashboardDto(
            revenueByMonth[^1].NetRevenue,
            yearToDate,
            balances.Sum(x => x.Balance),
            balances.Where(x => x.DaysOverdue > 0).Sum(x => x.Balance),
            revenueByMonth,
            topCustomers,
            outstanding);
    }
}
