using FluentValidation;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed record CreateCreditNoteInput(string Reason, IReadOnlyList<CreditNoteLineInput> Lines);

public sealed record CreditNoteLineInput(int LineNumber, decimal Quantity);

public sealed class CreateCreditNoteInputValidator : AbstractValidator<CreateCreditNoteInput>
{
    public CreateCreditNoteInputValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.LineNumber).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}

/// <summary>Creates a draft credit note against an issued invoice; it is issued like any other draft.</summary>
public sealed class CreateCreditNote(IInvoiceRepository invoices, IUnitOfWork unitOfWork)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid originalId, CreateCreditNoteInput input, CancellationToken cancellationToken)
    {
        var original = await invoices.GetByIdAsync(originalId, cancellationToken);
        if (original is null)
        {
            return InvoiceErrors.NotFound(originalId);
        }

        var credited = await invoices.GetCreditedAmountAsync(originalId, cancellationToken);
        var creditNote = Invoice.CreateCreditNote(
            original,
            input.Reason,
            input.Lines.Select(line => new CreditNoteLine(line.LineNumber, line.Quantity)).ToList(),
            credited);

        if (creditNote.IsFailure)
        {
            return creditNote.Error;
        }

        invoices.Add(creditNote.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return creditNote.Value.ToDto();
    }
}
