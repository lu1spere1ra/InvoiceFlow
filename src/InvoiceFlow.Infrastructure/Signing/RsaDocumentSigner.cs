using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using InvoiceFlow.Domain.Invoices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.Signing;

/// <summary>
/// RSA-SHA1 (PKCS#1 v1.5) signatures with a 1024-bit key, base64-encoded: the algorithm and size the AT
/// specification prescribes, which also keeps the hash within the 172 characters SAF-T allows (ADR 0004).
/// SHA-1 and 1024-bit RSA would not be chosen for new designs; they are kept here for format fidelity.
/// </summary>
internal sealed class RsaDocumentSigner : IDocumentSigner, IDisposable
{
    private readonly RSA _rsa;

    public RsaDocumentSigner(IOptions<SigningOptions> options, ILogger<RsaDocumentSigner> logger)
    {
        var settings = options.Value;
        KeyVersion = settings.KeyVersion;
        _rsa = RSA.Create();

        if (File.Exists(settings.KeyPath))
        {
            _rsa.ImportFromPem(File.ReadAllText(settings.KeyPath));
            return;
        }

        if (!settings.GenerateIfMissing)
        {
            throw new InvalidOperationException($"Signing key not found at '{settings.KeyPath}'.");
        }

        logger.LogWarning("No signing key at {KeyPath}; generating a new demo key", settings.KeyPath);
        _rsa.KeySize = AtKeySize;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings.KeyPath))!);
        File.WriteAllText(settings.KeyPath, _rsa.ExportPkcs8PrivateKeyPem());
    }

    [SuppressMessage("Security", "CA5385:Use RSA with sufficient key size", Justification = "Size mandated by the AT specification.")]
    private static int AtKeySize => 1024;

    public string KeyVersion { get; }

    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms", Justification = "SHA-1 mandated by the AT specification.")]
    public string Sign(string message) =>
        Convert.ToBase64String(_rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));

    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms", Justification = "SHA-1 mandated by the AT specification.")]
    public bool Verify(string message, string signature)
    {
        Span<byte> buffer = stackalloc byte[512];
        return Convert.TryFromBase64String(signature, buffer, out var length) &&
               _rsa.VerifyData(Encoding.UTF8.GetBytes(message), buffer[..length], HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    public void Dispose() => _rsa.Dispose();
}
