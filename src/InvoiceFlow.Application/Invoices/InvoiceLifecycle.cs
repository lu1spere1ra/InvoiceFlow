using FluentValidation;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed record MarkPaidInput(DateOnly? PaidOn);

public sealed record VoidInput(string Reason);

public sealed class VoidInputValidator : AbstractValidator<VoidInput>
{
    public VoidInputValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
}

public sealed class MarkInvoicePaid(IInvoiceRepository invoices, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, MarkPaidInput input, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        var today = timeProvider.TodayInPortugal();
        var paidOn = input.PaidOn ?? today;
        if (paidOn > today)
        {
            return InvoiceErrors.PaymentInFuture;
        }

        var result = invoice.MarkAsPaid(paidOn);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice.ToDto();
    }
}

public sealed class VoidInvoice(IInvoiceRepository invoices, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, VoidInput input, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        var credited = await invoices.GetCreditedAmountAsync(id, cancellationToken);
        var result = invoice.Void(input.Reason, timeProvider.GetUtcNow(), credited);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invoice.ToDto();
    }
}
