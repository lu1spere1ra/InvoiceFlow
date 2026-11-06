using System.Globalization;

namespace InvoiceFlow.Domain.Invoices;

/// <summary>
/// The document hash chain, modelled on the AT rules (Portaria 363/2010): each document signs
/// <c>InvoiceDate;SystemEntryDate;DocumentNo;GrossTotal;PreviousHash</c>, so altering or removing any
/// document breaks every signature after it.
/// </summary>
public static class HashChain
{
    public static string Message(DateOnly invoiceDate, DateTimeOffset systemEntryDate, string documentNumber, decimal grossTotal, string? previousHash) =>
        string.Join(
            ';',
            invoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            systemEntryDate.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
            documentNumber,
            grossTotal.ToString("0.00", CultureInfo.InvariantCulture),
            previousHash ?? "");

    /// <summary>The four characters printed on documents: positions 1, 11, 21 and 31 of the hash.</summary>
    public static string Excerpt(string? hash) =>
        hash is { Length: >= 31 } ? string.Concat(hash[0], hash[10], hash[20], hash[30]) : "";

    /// <summary>
    /// Verifies a series: documents must be numbered 1..n without gaps and every signature must match
    /// the recomputed message chained to the previous document.
    /// </summary>
    /// <returns>The number of the first broken document, or <c>null</c> when the chain is intact.</returns>
    public static int? FindFirstBreak(IEnumerable<Invoice> documentsInSeries, IDocumentSigner signer)
    {
        string? previousHash = null;
        var expectedNumber = 1;

        foreach (var document in documentsInSeries.OrderBy(d => d.Number))
        {
            if (document.Number != expectedNumber || document.Hash is null)
            {
                return expectedNumber;
            }

            var message = Message(document.IssueDate!.Value, document.SystemEntryDate!.Value, document.DocumentNumber!, document.GrossTotal, previousHash);
            if (!signer.Verify(message, document.Hash))
            {
                return document.Number;
            }

            previousHash = document.Hash;
            expectedNumber++;
        }

        return null;
    }
}
