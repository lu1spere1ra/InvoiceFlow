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

    /// <summary>Soft delete: the customer disappears from lists but documents keep referencing it.</summary>
    void Remove(Customer customer);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    void Add(Product product);

    /// <summary>Soft delete.</summary>
    void Remove(Product product);
}

public interface ITaxRateRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, TaxRate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}

public interface IDocumentSeriesRepository
{
    Task<DocumentSeries?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DocumentSeries?> GetActiveAsync(DocumentType documentType, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(DocumentType documentType, string code, CancellationToken cancellationToken);

    void Add(DocumentSeries series);
}

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Gross total of issued (non-voided) credit notes, against an invoice.</summary>
    Task<decimal> GetCreditedAmountAsync(Guid originalInvoiceId, CancellationToken cancellationToken);

    /// <summary>Every issued document of a series (including voided ones), for hash-chain verification.</summary>
    Task<IReadOnlyList<Invoice>> ListIssuedInSeriesAsync(Guid seriesId, CancellationToken cancellationToken);

    void Add(Invoice invoice);

    void Remove(Invoice invoice);
}
