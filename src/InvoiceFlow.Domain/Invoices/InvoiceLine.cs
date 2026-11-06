using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Domain.Invoices;

/// <summary>
/// A document line. Product and VAT data are copied (snapshotted) so that later catalog or rate
/// changes never alter a document. Lines are only created through <see cref="Invoice"/>.
/// </summary>
public sealed class InvoiceLine
{
    internal InvoiceLine(
        int lineNumber,
        Product product,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        TaxRate taxRate,
        ExemptionReason? exemption)
    {
        LineNumber = lineNumber;
        ProductId = product.Id;
        ProductCode = product.Code;
        Description = description;
        Unit = product.Unit;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        TaxCode = taxRate.Code;
        TaxPercentage = taxRate.Percentage;
        ExemptionCode = exemption?.Code;
        ExemptionReason = exemption?.Description;
        NetAmount = Money.Round(quantity * unitPrice * (1 - (discountPercent / 100m)));
    }

    // Required by EF Core.
    private InvoiceLine()
    {
        ProductCode = null!;
        Description = null!;
        Unit = null!;
    }

    public int LineNumber { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductCode { get; private set; }

    public string Description { get; private set; }

    public string Unit { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal DiscountPercent { get; private set; }

    public TaxRateCode TaxCode { get; private set; }

    public decimal TaxPercentage { get; private set; }

    public string? ExemptionCode { get; private set; }

    public string? ExemptionReason { get; private set; }

    /// <summary>Quantity × unit price − discount, rounded to cents. VAT is computed per rate on the document.</summary>
    public decimal NetAmount { get; private set; }

    internal void Renumber(int lineNumber) => LineNumber = lineNumber;
}
