namespace InvoiceFlow.Domain.Taxes;

/// <summary>
/// Legal reason for not charging VAT, using the AT exemption codes (<c>TaxExemptionCode</c> in SAF-T).
/// Only the most common codes are listed.
/// </summary>
public sealed record ExemptionReason(string Code, string Description)
{
    public static IReadOnlyList<ExemptionReason> All { get; } =
    [
        new("M01", "Artigo 16.º, n.º 6 do CIVA"),
        new("M02", "Artigo 6.º do Decreto-Lei n.º 198/90, de 19 de junho"),
        new("M04", "Isento artigo 13.º do CIVA"),
        new("M05", "Isento artigo 14.º do CIVA"),
        new("M06", "Isento artigo 15.º do CIVA"),
        new("M07", "Isento artigo 9.º do CIVA"),
        new("M10", "IVA - regime de isenção (artigo 53.º do CIVA)"),
        new("M16", "Isento artigo 14.º do RITI"),
        new("M99", "Não sujeito ou não tributado"),
    ];

    public static ExemptionReason? Find(string? code) =>
        All.FirstOrDefault(reason => string.Equals(reason.Code, code, StringComparison.OrdinalIgnoreCase));
}
