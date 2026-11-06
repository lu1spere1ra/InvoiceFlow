using FluentValidation;

namespace InvoiceFlow.Application.Customers;

/// <summary>Request body shared by create and update.</summary>
public sealed record CustomerInput(
    string Name,
    string Nif,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string? Email);

/// <summary>
/// Checks the request shape (required fields, lengths, formats) and gives friendly field-level errors.
/// Business invariants such as the NIF check digit are still enforced by the domain.
/// </summary>
public sealed class CustomerInputValidator : AbstractValidator<CustomerInput>
{
    public CustomerInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Nif).NotEmpty().Matches(@"^\d{9}$").WithMessage("The NIF must have 9 digits.");
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().Length(2);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{4}-\d{3}$").WithMessage("Portuguese postal codes use the format 0000-000.")
            .When(x => x.Country == "PT");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Email));
    }
}
