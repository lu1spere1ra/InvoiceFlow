using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Domain.Taxes;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/tax-rates")]
public sealed class TaxRatesController(ICatalogQueries queries) : ApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TaxRateDto>> TaxRates(CancellationToken cancellationToken) =>
        queries.ListTaxRatesAsync(cancellationToken);

    [HttpGet("exemption-reasons")]
    public IReadOnlyList<ExemptionReason> ExemptionReasons() => ExemptionReason.All;
}
