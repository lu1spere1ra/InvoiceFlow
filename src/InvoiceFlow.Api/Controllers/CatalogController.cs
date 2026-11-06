using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Domain.Taxes;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api")]
public sealed class CatalogController(ICatalogQueries queries) : ApiControllerBase
{
    [HttpGet("tax-rates")]
    public Task<IReadOnlyList<TaxRateDto>> TaxRates(CancellationToken cancellationToken) =>
        queries.ListTaxRatesAsync(cancellationToken);

    [HttpGet("tax-rates/exemption-reasons")]
    public IReadOnlyList<ExemptionReason> ExemptionReasons() => ExemptionReason.All;

    [HttpGet("products")]
    public Task<IReadOnlyList<ProductDto>> Products(CancellationToken cancellationToken) =>
        queries.ListProductsAsync(cancellationToken);
}
