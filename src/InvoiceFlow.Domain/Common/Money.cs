namespace InvoiceFlow.Domain.Common;

public static class Money
{
    /// <summary>
    /// Rounds a monetary amount to cents using "round half away from zero", the convention used on
    /// Portuguese invoices (0.005 → 0.01). <see cref="Math.Round(decimal)"/> defaults to banker's rounding.
    /// </summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
