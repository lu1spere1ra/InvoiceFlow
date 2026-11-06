using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

internal sealed class InvoiceQueries(AppDbContext db, TimeProvider timeProvider) : IInvoiceQueries
{
    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // The detail view needs computed values (VAT summary), so it reuses the aggregate's mapping.
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var credited = await db.Invoices
            .Where(i => i.OriginalInvoiceId == id && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Paid))
            .SumAsync(i => i.GrossTotal, cancellationToken);

        return invoice.ToDto(credited);
    }

    public async Task<PagedResult<InvoiceSummaryDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var invoices = db.Invoices.AsNoTracking();

        if (query.Type is { } type)
        {
            invoices = invoices.Where(i => i.Type == type);
        }

        if (query.Status is { } status)
        {
            invoices = invoices.Where(i => i.Status == status);
        }

        if (query.CustomerId is { } customerId)
        {
            invoices = invoices.Where(i => i.CustomerId == customerId);
        }

        if (query.From is { } from)
        {
            invoices = invoices.Where(i => i.IssueDate >= from);
        }

        if (query.To is { } to)
        {
            invoices = invoices.Where(i => i.IssueDate <= to);
        }

        if (query.OverdueOnly)
        {
            var today = timeProvider.TodayInPortugal();
            invoices = invoices.Where(i => i.Status == InvoiceStatus.Issued && i.Type == DocumentType.Invoice && i.DueDate < today);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            invoices = invoices.Where(i =>
                i.DocumentNumber!.Contains(search) || i.Customer.Name.Contains(search) || i.Customer.Nif.StartsWith(search));
        }

        var total = await invoices.CountAsync(cancellationToken);

        // Projection straight to the DTO: only the listed columns are read and lines are never loaded.
        var items = await Sort(invoices, query)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(i => new InvoiceSummaryDto(
                i.Id, i.Type, i.Status, i.DocumentNumber, i.Customer.Name, i.Customer.Nif, i.IssueDate, i.DueDate, i.GrossTotal))
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceSummaryDto>(items, query.Page, query.PageSize, total);
    }

    /// <summary>
    /// Sorting from a closed set of options (no dynamic expressions from the client). Drafts, which have no
    /// date or number, come first when sorting by date so work in progress is easy to find.
    /// </summary>
    private static IOrderedQueryable<Invoice> Sort(IQueryable<Invoice> invoices, InvoiceListQuery query) =>
        (query.SortBy, query.Descending) switch
        {
            (InvoiceSort.DueDate, false) => invoices.OrderBy(i => i.DueDate).ThenBy(i => i.Number),
            (InvoiceSort.DueDate, true) => invoices.OrderByDescending(i => i.DueDate).ThenByDescending(i => i.Number),
            (InvoiceSort.Number, false) => invoices.OrderBy(i => i.DocumentNumber),
            (InvoiceSort.Number, true) => invoices.OrderByDescending(i => i.DocumentNumber),
            (InvoiceSort.Customer, false) => invoices.OrderBy(i => i.Customer.Name).ThenByDescending(i => i.IssueDate),
            (InvoiceSort.Customer, true) => invoices.OrderByDescending(i => i.Customer.Name).ThenByDescending(i => i.IssueDate),
            (InvoiceSort.Total, false) => invoices.OrderBy(i => i.GrossTotal),
            (InvoiceSort.Total, true) => invoices.OrderByDescending(i => i.GrossTotal),
            (_, false) => invoices.OrderBy(i => i.IssueDate == null).ThenBy(i => i.IssueDate).ThenBy(i => i.SystemEntryDate),
            _ => invoices.OrderByDescending(i => i.IssueDate == null).ThenByDescending(i => i.IssueDate).ThenByDescending(i => i.SystemEntryDate),
        };
}
