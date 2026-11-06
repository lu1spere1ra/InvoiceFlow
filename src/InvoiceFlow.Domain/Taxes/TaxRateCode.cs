namespace InvoiceFlow.Domain.Taxes;

/// <summary>VAT rate categories, named after the SAF-T (PT) <c>TaxCode</c> values.</summary>
public enum TaxRateCode
{
    /// <summary>NOR — normal rate (23% on the mainland).</summary>
    Normal,

    /// <summary>INT — intermediate rate (13%).</summary>
    Intermediate,

    /// <summary>RED — reduced rate (6%).</summary>
    Reduced,

    /// <summary>ISE — exempt; every exempt line must state a legal exemption reason.</summary>
    Exempt,
}

public static class TaxRateCodeExtensions
{
    public static string ToSaftCode(this TaxRateCode code) => code switch
    {
        TaxRateCode.Normal => "NOR",
        TaxRateCode.Intermediate => "INT",
        TaxRateCode.Reduced => "RED",
        TaxRateCode.Exempt => "ISE",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
