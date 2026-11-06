namespace InvoiceFlow.Application.Common;

/// <summary>The issuing company, printed on documents and written to the SAF-T header. From configuration.</summary>
public sealed record CompanyProfile
{
    public string Name { get; init; } = "";

    public string Nif { get; init; } = "";

    public string Street { get; init; } = "";

    public string PostalCode { get; init; } = "";

    public string City { get; init; } = "";

    public string Country { get; init; } = "PT";

    public string? Email { get; init; }

    public string? Phone { get; init; }

    /// <summary>
    /// AT software certificate number. "0" because InvoiceFlow is not certified; printed in the QR code
    /// (field R) and SAF-T header exactly as a real program would print its number.
    /// </summary>
    public string SoftwareCertificateNumber { get; init; } = "0";
}
