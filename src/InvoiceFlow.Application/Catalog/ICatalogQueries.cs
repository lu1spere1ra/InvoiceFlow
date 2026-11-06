using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Catalog;

public sealed record TaxRateDto(Guid Id, TaxRateCode Code, string Description, decimal Percentage);

public sealed record ProductDto(
    Guid Id, string Code, string Description, ProductType Type, string Unit, decimal UnitPrice, Guid TaxRateId);

public interface ICatalogQueries
{
    Task<IReadOnlyList<TaxRateDto>> ListTaxRatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDto>> ListProductsAsync(CancellationToken cancellationToken);
}
