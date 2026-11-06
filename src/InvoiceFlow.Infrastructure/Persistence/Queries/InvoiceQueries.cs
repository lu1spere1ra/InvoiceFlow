using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

internal sealed class InvoiceQueries(AppDbContext db) : IInvoiceQueries
{
    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // The detail view needs computed values (VAT summary), so it reuses the aggregate's mapping.
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return invoice?.ToDto();
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

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            invoices = invoices.Where(i =>
                i.DocumentNumber!.Contains(search) || i.Customer.Name.Contains(search) || i.Customer.Nif.StartsWith(search));
        }

        var total = await invoices.CountAsync(cancellationToken);

        // Projection straight to the DTO: only the listed columns are read and lines are never loaded.
        var items = await invoices
            .OrderByDescending(i => i.IssueDate == null)
            .ThenByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.Number)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(i => new InvoiceSummaryDto(
                i.Id, i.Type, i.Status, i.DocumentNumber, i.Customer.Name, i.Customer.Nif, i.IssueDate, i.DueDate, i.GrossTotal))
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceSummaryDto>(items, query.Page, query.PageSize, total);
    }
}
