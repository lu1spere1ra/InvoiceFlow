using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Domain.UnitTests.Customers;

public sealed class CustomerTests
{
    private static readonly Address Address = new("Rua Direita 1", "4000-001", "Porto");

    [Fact]
    public void Create_trims_name_and_normalises_empty_email()
    {
        var customer = Customer.Create("  Padaria Central  ", "503504564", Address, " ").Value;

        customer.Name.ShouldBe("Padaria Central");
        customer.Email.ShouldBeNull();
    }

    [Fact]
    public void Create_fails_without_name() =>
        Customer.Create(" ", "503504564", Address).Error.ShouldBe(CustomerErrors.NameRequired);

    [Fact]
    public void Update_keeps_previous_data_when_nif_is_invalid()
    {
        var customer = Customer.Create("Padaria Central", "503504564", Address).Value;

        var result = customer.Update("Outro Nome", "111111111", Address, null);

        result.Error.ShouldBe(CustomerErrors.InvalidNif);
        customer.Name.ShouldBe("Padaria Central");
    }
}
