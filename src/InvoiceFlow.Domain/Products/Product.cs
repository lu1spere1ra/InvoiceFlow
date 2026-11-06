using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Products;

/// <summary>An article or service that can be invoiced.</summary>
public sealed class Product : Entity
{
    private Product(string code, string description, ProductType type, string unit, decimal unitPrice, Guid taxRateId)
    {
        Code = code;
        Description = description;
        Type = type;
        Unit = unit;
        UnitPrice = unitPrice;
        TaxRateId = taxRateId;
    }

    // Required by EF Core.
    private Product()
    {
        Code = null!;
        Description = null!;
        Unit = null!;
    }

    public string Code { get; private set; }

    public string Description { get; private set; }

    public ProductType Type { get; private set; }

    public string Unit { get; private set; }

    public decimal UnitPrice { get; private set; }

    /// <summary>Default VAT rate suggested when the product is added to a document.</summary>
    public Guid TaxRateId { get; private set; }

    public static Result<Product> Create(
        string code, string description, ProductType type, string unit, decimal unitPrice, Guid taxRateId)
    {
        var error = Validate(code, description, unitPrice);
        return error is null
            ? new Product(code.Trim().ToUpperInvariant(), description.Trim(), type, unit, unitPrice, taxRateId)
            : error;
    }

    public Result Update(string description, ProductType type, string unit, decimal unitPrice, Guid taxRateId)
    {
        var error = Validate(Code, description, unitPrice);
        if (error is not null)
        {
            return error;
        }

        Description = description.Trim();
        Type = type;
        Unit = unit;
        UnitPrice = unitPrice;
        TaxRateId = taxRateId;
        return Result.Success();
    }

    private static Error? Validate(string code, string description, decimal unitPrice) =>
        string.IsNullOrWhiteSpace(code) ? ProductErrors.CodeRequired
        : string.IsNullOrWhiteSpace(description) ? ProductErrors.DescriptionRequired
        : unitPrice < 0 ? ProductErrors.NegativePrice
        : null;
}
