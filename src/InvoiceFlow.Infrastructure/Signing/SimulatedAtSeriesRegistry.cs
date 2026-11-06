using System.Security.Cryptography;
using InvoiceFlow.Application.Series;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Infrastructure.Signing;

/// <summary>
/// Stands in for the AT series communication web service, which returns the validation code of a series.
/// Real integration needs a certified program and AT credentials, so this demo generates the code locally.
/// </summary>
internal sealed class SimulatedAtSeriesRegistry : IAtSeriesRegistry
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public Task<string> RegisterAsync(DocumentType documentType, string seriesCode, CancellationToken cancellationToken) =>
        Task.FromResult(RandomNumberGenerator.GetString(Alphabet, 8));
}
