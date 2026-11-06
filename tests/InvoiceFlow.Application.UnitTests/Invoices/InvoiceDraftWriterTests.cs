using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;
using NSubstitute;

namespace InvoiceFlow.Application.UnitTests.Invoices;

public sealed class InvoiceDraftWriterTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ITaxRateRepository _taxRates = Substitute.For<ITaxRateRepository>();
    private readonly TaxRate _normal = TaxRate.Create(TaxRateCode.Normal, "Normal", 23m).Value;
    private readonly TaxRate _reduced = TaxRate.Create(TaxRateCode.Reduced, "Reduzida", 6m).Value;
    private readonly Product _product;

    public InvoiceDraftWriterTests()
    {
        _product = Product.Create("P1", "Pão", ProductType.Goods, "UN", 2.50m, _normal.Id).Value;
        _products.GetByIdsAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, Product> { [_product.Id] = _product });
        _taxRates.GetByIdsAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, TaxRate>
        {
            [_normal.Id] = _normal,
            [_reduced.Id] = _reduced,
        });
    }

    private InvoiceDraftWriter Writer => new(_products, _taxRates);

    [Fact]
    public async Task Uses_product_defaults_when_line_omits_price_and_rate()
    {
        var invoice = Draft();

        var result = await Writer.ReplaceLinesAsync(invoice, [new InvoiceLineInput(_product.Id, 2)], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        invoice.Lines.ShouldHaveSingleItem().UnitPrice.ShouldBe(2.50m);
        invoice.Lines[0].TaxPercentage.ShouldBe(23m);
    }

    [Fact]
    public async Task Line_values_override_product_defaults()
    {
        var invoice = Draft();

        await Writer.ReplaceLinesAsync(
            invoice, [new InvoiceLineInput(_product.Id, 1, UnitPrice: 3m, TaxRateId: _reduced.Id)], TestContext.Current.CancellationToken);

        invoice.Lines[0].UnitPrice.ShouldBe(3m);
        invoice.Lines[0].TaxPercentage.ShouldBe(6m);
    }

    [Fact]
    public async Task Fails_for_an_unknown_product()
    {
        var unknown = Guid.NewGuid();

        var result = await Writer.ReplaceLinesAsync(Draft(), [new InvoiceLineInput(unknown, 1)], TestContext.Current.CancellationToken);

        result.Error.ShouldBe(ProductErrors.NotFound(unknown));
    }

    [Fact]
    public async Task Replaces_existing_lines()
    {
        var invoice = Draft();
        await Writer.ReplaceLinesAsync(invoice, [new InvoiceLineInput(_product.Id, 1), new InvoiceLineInput(_product.Id, 2)], TestContext.Current.CancellationToken);

        await Writer.ReplaceLinesAsync(invoice, [new InvoiceLineInput(_product.Id, 5)], TestContext.Current.CancellationToken);

        invoice.Lines.ShouldHaveSingleItem().Quantity.ShouldBe(5);
    }

    private static Invoice Draft() =>
        Invoice.CreateDraft(
            DocumentType.Invoice,
            Customer.Create("Padaria", "503504564", new Address("Rua 1", "4000-001", "Porto")).Value).Value;
}
