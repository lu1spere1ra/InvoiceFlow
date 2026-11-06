using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Invoices;

/// <summary>
/// Shared by create and update: resolves products and VAT rates in two queries and replaces the
/// draft's lines. The aggregate still decides whether each line is valid.
/// </summary>
public sealed class InvoiceDraftWriter(IProductRepository products, ITaxRateRepository taxRates)
{
    public async Task<Result> ReplaceLinesAsync(
        Invoice invoice, IReadOnlyList<InvoiceLineInput> lines, CancellationToken cancellationToken)
    {
        var productsById = await products.GetByIdsAsync(lines.Select(line => line.ProductId).Distinct(), cancellationToken);

        var missingProduct = lines.FirstOrDefault(line => !productsById.ContainsKey(line.ProductId));
        if (missingProduct is not null)
        {
            return ProductErrors.NotFound(missingProduct.ProductId);
        }

        var taxRateIds = lines.Select(line => line.TaxRateId ?? productsById[line.ProductId].TaxRateId).Distinct();
        var taxRatesById = await taxRates.GetByIdsAsync(taxRateIds, cancellationToken);

        var cleared = invoice.ClearLines();
        if (cleared.IsFailure)
        {
            return cleared;
        }

        foreach (var line in lines)
        {
            var product = productsById[line.ProductId];
            var taxRateId = line.TaxRateId ?? product.TaxRateId;
            if (!taxRatesById.TryGetValue(taxRateId, out var taxRate))
            {
                return TaxErrors.NotFound(taxRateId);
            }

            var added = invoice.AddLine(
                product,
                taxRate,
                line.Quantity,
                line.UnitPrice ?? product.UnitPrice,
                line.DiscountPercent,
                line.ExemptionCode,
                line.Description);

            if (added.IsFailure)
            {
                return added;
            }
        }

        return Result.Success();
    }
}
