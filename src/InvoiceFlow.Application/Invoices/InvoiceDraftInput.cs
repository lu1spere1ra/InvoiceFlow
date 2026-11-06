using FluentValidation;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed record CreateInvoiceDraftInput(
    DocumentType Type,
    Guid CustomerId,
    int PaymentTermDays,
    IReadOnlyList<InvoiceLineInput> Lines);

public sealed record UpdateInvoiceDraftInput(
    Guid CustomerId,
    int PaymentTermDays,
    IReadOnlyList<InvoiceLineInput> Lines);

/// <param name="ProductId">Product or service being invoiced.</param>
/// <param name="Quantity">Quantity, up to 4 decimal places.</param>
/// <param name="UnitPrice">Defaults to the product price when omitted.</param>
/// <param name="DiscountPercent">Line discount, 0–100.</param>
/// <param name="TaxRateId">Defaults to the product's VAT rate when omitted.</param>
/// <param name="ExemptionCode">AT exemption code (e.g. M07), required for exempt VAT rates.</param>
/// <param name="Description">Defaults to the product description when omitted.</param>
public sealed record InvoiceLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitPrice = null,
    decimal DiscountPercent = 0,
    Guid? TaxRateId = null,
    string? ExemptionCode = null,
    string? Description = null);

public sealed class CreateInvoiceDraftInputValidator : AbstractValidator<CreateInvoiceDraftInput>
{
    public CreateInvoiceDraftInputValidator()
    {
        RuleFor(x => x.Type).IsInEnum()
            .NotEqual(DocumentType.CreditNote).WithMessage("Credit notes are created from an issued invoice.");
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Lines).NotNull().Must(lines => lines.Count <= 200).WithMessage("A document can have at most 200 lines.");
        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineInputValidator());
    }
}

public sealed class UpdateInvoiceDraftInputValidator : AbstractValidator<UpdateInvoiceDraftInput>
{
    public UpdateInvoiceDraftInputValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Lines).NotNull().Must(lines => lines.Count <= 200).WithMessage("A document can have at most 200 lines.");
        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineInputValidator());
    }
}

public sealed class InvoiceLineInputValidator : AbstractValidator<InvoiceLineInput>
{
    public InvoiceLineInputValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, ignoreTrailingZeros: true);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Description).MaximumLength(200);
    }
}
