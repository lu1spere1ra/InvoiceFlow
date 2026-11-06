using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NifExistsAsync(string nif, Guid? excludingCustomerId, CancellationToken cancellationToken)
    {
        var value = Nif.Create(nif).Value;
        return db.Customers.AnyAsync(c => c.Nif == value && c.Id != excludingCustomerId, cancellationToken);
    }

    public void Add(Customer customer) => db.Customers.Add(customer);
}

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<IReadOnlyDictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
}

internal sealed class TaxRateRepository(AppDbContext db) : ITaxRateRepository
{
    public async Task<IReadOnlyDictionary<Guid, TaxRate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        await db.TaxRates.Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
}

internal sealed class DocumentSeriesRepository(AppDbContext db) : IDocumentSeriesRepository
{
    public Task<DocumentSeries?> GetActiveAsync(DocumentType documentType, CancellationToken cancellationToken) =>
        db.DocumentSeries
            .Where(s => s.DocumentType == documentType && s.IsActive)
            .OrderByDescending(s => s.Code)
            .FirstOrDefaultAsync(cancellationToken);
}

internal sealed class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);

    public void Remove(Invoice invoice) => db.Invoices.Remove(invoice);
}
