using System.Security.Cryptography;
using System.Text;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Domain.UnitTests;

/// <summary>Deterministic stand-in for the RSA signer: a keyed SHA-256, enough to test the chain logic.</summary>
internal sealed class FakeSigner : IDocumentSigner
{
    public string KeyVersion => "1";

    public string Sign(string message) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("test-key|" + message)));

    public bool Verify(string message, string signature) => Sign(message) == signature;
}
