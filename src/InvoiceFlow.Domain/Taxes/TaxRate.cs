using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Taxes;

public sealed class TaxRate : Entity
{
    private TaxRate(TaxRateCode code, string description, decimal percentage)
    {
        Code = code;
        Description = description;
        Percentage = percentage;
    }

    // Required by EF Core.
    private TaxRate() => Description = null!;

    public TaxRateCode Code { get; private set; }

    public string Description { get; private set; }

    public decimal Percentage { get; private set; }

    public bool IsExempt => Code == TaxRateCode.Exempt;

    public static Result<TaxRate> Create(TaxRateCode code, string description, decimal percentage)
    {
        if (percentage is < 0 or > 100)
        {
            return TaxErrors.InvalidPercentage;
        }

        if (code == TaxRateCode.Exempt && percentage != 0)
        {
            return TaxErrors.ExemptRateMustBeZero;
        }

        return new TaxRate(code, description, percentage);
    }
}
