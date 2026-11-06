namespace InvoiceFlow.Domain.Invoices;

/// <summary>
/// Signs the hash-chain message of a document. The domain decides <em>what</em> is signed; how (RSA key,
/// algorithm) is an infrastructure detail (ADR 0004).
/// </summary>
public interface IDocumentSigner
{
    /// <summary>Version of the signing key, written to SAF-T as <c>HashControl</c>.</summary>
    string KeyVersion { get; }

    string Sign(string message);

    bool Verify(string message, string signature);
}
