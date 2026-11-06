namespace InvoiceFlow.Infrastructure.Signing;

public sealed class SigningOptions
{
    public const string SectionName = "Signing";

    /// <summary>PEM file with the RSA private key.</summary>
    public string KeyPath { get; init; } = "keys/signing-key.pem";

    /// <summary>Creates a key on first start when the file is missing. For demos and development only.</summary>
    public bool GenerateIfMissing { get; init; }

    /// <summary>Written to SAF-T <c>HashControl</c>; bump it when the key is rotated.</summary>
    public string KeyVersion { get; init; } = "1";
}
