namespace InvoiceFlow.Application.Dashboard;

/// <summary>Net revenue of one month: invoices and invoice-receipts minus credit notes, voided excluded.</summary>
public sealed record MonthlyRevenueDto(int Year, int Month, decimal NetRevenue, int DocumentCount);

public sealed record TopCustomerDto(string Nif, string Name, decimal NetRevenue, int DocumentCount);

/// <summary>Unpaid invoice balances grouped by days past the due date.</summary>
public sealed record AgingBucketDto(string Label, int MinDaysOverdue, int? MaxDaysOverdue, decimal Amount, int InvoiceCount);

public sealed record DashboardDto(
    decimal RevenueThisMonth,
    decimal RevenueYearToDate,
    decimal OutstandingTotal,
    decimal OverdueTotal,
    IReadOnlyList<MonthlyRevenueDto> RevenueByMonth,
    IReadOnlyList<TopCustomerDto> TopCustomers,
    IReadOnlyList<AgingBucketDto> Outstanding);

public interface IDashboardQueries
{
    /// <summary>Figures for the 12 months up to and including the current one (Portuguese calendar).</summary>
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
