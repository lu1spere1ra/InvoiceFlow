using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed class CreateInvoiceDraft(
    ICustomerRepository customers,
    IInvoiceRepository invoices,
    InvoiceDraftWriter draftWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<InvoiceDto>> HandleAsync(CreateInvoiceDraftInput input, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(input.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(input.CustomerId);
        }

        var invoice = Invoice.CreateDraft(input.Type, customer, input.PaymentTermDays);
        if (invoice.IsFailure)
        {
            return invoice.Error;
        }

        var lines = await draftWriter.ReplaceLinesAsync(invoice.Value, input.Lines, cancellationToken);
        if (lines.IsFailure)
        {
            return lines.Error;
        }

        invoices.Add(invoice.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice.Value.ToDto();
    }
}
