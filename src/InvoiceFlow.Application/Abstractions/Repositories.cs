using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Abstractions;

// Write-side ports: one small repository per aggregate, returning tracked domain objects.
// Read-side queries return DTOs and live next to their feature (e.g. IInvoiceQueries). See ADR 0006.

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NifExistsAsync(string nif, Guid? excludingCustomerId, CancellationToken cancellationToken);

    void Add(Customer customer);
}

public interface IProductRepository
{
    Task<IReadOnlyDictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}

public interface ITaxRateRepository
{
    Task<IReadOnlyDictionary<Guid, TaxRate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}

public interface IDocumentSeriesRepository
{
    Task<DocumentSeries?> GetActiveAsync(DocumentType documentType, CancellationToken cancellationToken);
}

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Invoice invoice);

    void Remove(Invoice invoice);
}
