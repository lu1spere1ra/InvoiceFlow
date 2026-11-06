using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

/// <summary>Drafts have no fiscal meaning and can be deleted; issued documents never can.</summary>
public sealed class DeleteInvoiceDraft(IInvoiceRepository invoices, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        if (!invoice.IsDraft)
        {
            return InvoiceErrors.NotDraft;
        }

        invoices.Remove(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
