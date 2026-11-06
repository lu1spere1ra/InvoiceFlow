using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Domain.Customers;
using NSubstitute;

namespace InvoiceFlow.Application.UnitTests.Customers;

public sealed class CreateCustomerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCustomer _handler;

    public CreateCustomerTests() => _handler = new CreateCustomer(_customers, _unitOfWork);

    [Fact]
    public async Task Creates_and_saves_a_valid_customer()
    {
        var result = await _handler.HandleAsync(Input("503504564"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Nif.ShouldBe("503504564");
        _customers.Received(1).Add(Arg.Is<Customer>(c => c.Name == "Padaria Central"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_duplicate_nif()
    {
        _customers.NifExistsAsync("503504564", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(Input("503504564"), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(CustomerErrors.DuplicateNif);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_many_final_consumer_records()
    {
        _customers.NifExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(Input(Nif.FinalConsumerValue), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    private static CustomerInput Input(string nif) =>
        new("Padaria Central", nif, "Rua Direita 1", "4000-001", "Porto", "PT", null);
}
