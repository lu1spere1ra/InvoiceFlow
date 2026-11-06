using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Products;

public static class ProductErrors
{
    public static readonly Error CodeRequired = new("Product.CodeRequired", "The product code is required.");
    public static readonly Error DescriptionRequired = new("Product.DescriptionRequired", "The product description is required.");
    public static readonly Error NegativePrice = new("Product.NegativePrice", "The unit price cannot be negative.");

    public static Error NotFound(Guid id) => Error.NotFound("Product.NotFound", $"Product '{id}' was not found.");
}
