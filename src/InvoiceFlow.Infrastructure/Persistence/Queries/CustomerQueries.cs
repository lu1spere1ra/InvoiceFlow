using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

internal sealed class CustomerQueries(AppDbContext db) : ICustomerQueries
{
    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer?.ToDto();
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var customers = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var nif = Nif.Create(search);
            customers = nif.IsSuccess
                ? customers.Where(c => c.Nif == nif.Value)
                : customers.Where(c => c.Name.Contains(search));
        }

        var total = await customers.CountAsync(cancellationToken);
        var page = await customers
            .OrderBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(page.Select(c => c.ToDto()).ToList(), query.Page, query.PageSize, total);
    }
}
