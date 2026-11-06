using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed class UpdateInvoiceDraft(
    ICustomerRepository customers,
    IInvoiceRepository invoices,
    InvoiceDraftWriter draftWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, UpdateInvoiceDraftInput input, CancellationToken cancellationToken)
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

        if (invoice.CustomerId != input.CustomerId)
        {
            var customer = await customers.GetByIdAsync(input.CustomerId, cancellationToken);
            if (customer is null)
            {
                return CustomerErrors.NotFound(input.CustomerId);
            }

            invoice.ChangeCustomer(customer);
        }

        var result = invoice.ChangePaymentTerms(input.PaymentTermDays);
        if (result.IsSuccess)
        {
            result = await draftWriter.ReplaceLinesAsync(invoice, input.Lines, cancellationToken);
        }

        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice.ToDto();
    }
}
