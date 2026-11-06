using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Series;

namespace InvoiceFlow.Application.Invoices;

/// <summary>
/// Issues a draft with today's date using the active series for its type. The number is reserved in
/// memory and persisted together with the document in one transaction; a concurrent issue on the same
/// series fails with a conflict instead of producing a duplicate or a gap (ADR 0003).
/// </summary>
public sealed class IssueInvoice(
    IInvoiceRepository invoices,
    IDocumentSeriesRepository series,
    IUnitOfWork unitOfWork,
    IDocumentSigner signer,
    TimeProvider timeProvider)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        var activeSeries = await series.GetActiveAsync(invoice.Type, cancellationToken);
        if (activeSeries is null)
        {
            return SeriesErrors.NoActiveSeries(invoice.Type.ToCode());
        }

        var now = timeProvider.NowInPortugal();
        var issued = invoice.Issue(activeSeries, DateOnly.FromDateTime(now.DateTime), now, signer);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice.ToDto();
    }
}
