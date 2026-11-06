using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Taxes;

public static class TaxErrors
{
    public static readonly Error InvalidPercentage = new("Tax.InvalidPercentage", "The VAT percentage must be between 0 and 100.");
    public static readonly Error ExemptRateMustBeZero = new("Tax.ExemptRateMustBeZero", "An exempt VAT rate must be 0%.");

    public static Error NotFound(Guid id) => Error.NotFound("Tax.NotFound", $"VAT rate '{id}' was not found.");
}
