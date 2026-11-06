using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Domain.UnitTests.Customers;

public sealed class NifTests
{
    [Theory]
    [InlineData("503504564")] // company
    [InlineData("123456789")] // individual
    [InlineData("999999990")] // final consumer
    [InlineData("501 442 600")] // spaces are ignored
    public void Accepts_valid_nifs(string value) =>
        Nif.Create(value).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("503504565")] // wrong check digit
    [InlineData("12345678")] // too short
    [InlineData("1234567890")] // too long
    [InlineData("12345678A")] // not numeric
    [InlineData("400000000")] // invalid prefix
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_invalid_nifs(string? value) =>
        Nif.Create(value).Error.ShouldBe(CustomerErrors.InvalidNif);

    [Fact]
    public void Recognises_the_final_consumer_nif() =>
        Nif.Create("999999990").Value.IsFinalConsumer.ShouldBeTrue();
}
