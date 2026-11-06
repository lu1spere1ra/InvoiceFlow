using InvoiceFlow.Application.Customers;

namespace InvoiceFlow.Application.UnitTests.Customers;

public sealed class CustomerInputValidatorTests
{
    private readonly CustomerInputValidator _validator = new();

    [Fact]
    public void Accepts_a_complete_portuguese_customer() =>
        _validator.Validate(new CustomerInput("Padaria", "503504564", "Rua 1", "4000-001", "Porto", "PT", "a@b.pt"))
            .IsValid.ShouldBeTrue();

    [Fact]
    public void Requires_portuguese_postal_code_format_only_for_portugal()
    {
        _validator.Validate(new CustomerInput("Padaria", "503504564", "Rua 1", "4000", "Porto", "PT", null))
            .Errors.ShouldContain(error => error.PropertyName == nameof(CustomerInput.PostalCode));

        _validator.Validate(new CustomerInput("Bakery", "503504564", "Calle 1", "28001", "Madrid", "ES", null))
            .IsValid.ShouldBeTrue();
    }
}
