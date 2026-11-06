using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Customers;

/// <summary>
/// Portuguese tax identification number (Número de Identificação Fiscal), validated with the
/// official mod-11 check digit.
/// </summary>
public sealed record Nif
{
    /// <summary>NIF used for anonymous final consumers ("Consumidor final").</summary>
    public const string FinalConsumerValue = "999999990";

    private static readonly string[] ValidPrefixes =
        ["1", "2", "3", "5", "6", "8", "9", "45", "70", "71", "72", "74", "75", "77", "79", "90", "91", "98", "99"];

    private Nif(string value) => Value = value;

    public string Value { get; }

    public bool IsFinalConsumer => Value == FinalConsumerValue;

    public static Nif FinalConsumer { get; } = new(FinalConsumerValue);

    public static Result<Nif> Create(string? value)
    {
        var digits = value?.Replace(" ", "", StringComparison.Ordinal) ?? "";

        if (digits.Length != 9 || !digits.All(char.IsAsciiDigit))
        {
            return CustomerErrors.InvalidNif;
        }

        if (!ValidPrefixes.Any(prefix => digits.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return CustomerErrors.InvalidNif;
        }

        return HasValidCheckDigit(digits) ? new Nif(digits) : CustomerErrors.InvalidNif;
    }

    private static bool HasValidCheckDigit(string digits)
    {
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += (digits[i] - '0') * (9 - i);
        }

        var checkDigit = 11 - (sum % 11);
        if (checkDigit >= 10)
        {
            checkDigit = 0;
        }

        return checkDigit == digits[8] - '0';
    }

    public override string ToString() => Value;
}
