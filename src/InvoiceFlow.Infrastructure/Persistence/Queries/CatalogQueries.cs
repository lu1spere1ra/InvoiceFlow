using InvoiceFlow.Application.Catalog;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

internal sealed class CatalogQueries(AppDbContext db) : ICatalogQueries
{
    public async Task<IReadOnlyList<TaxRateDto>> ListTaxRatesAsync(CancellationToken cancellationToken) =>
        await db.TaxRates.AsNoTracking()
            .OrderByDescending(t => t.Percentage)
            .Select(t => new TaxRateDto(t.Id, t.Code, t.Description, t.Percentage))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> ListProductsAsync(CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new ProductDto(p.Id, p.Code, p.Description, p.Type, p.Unit, p.UnitPrice, p.TaxRateId))
            .ToListAsync(cancellationToken);
}
