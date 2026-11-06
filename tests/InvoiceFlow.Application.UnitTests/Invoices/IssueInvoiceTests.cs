using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;
using NSubstitute;

namespace InvoiceFlow.Application.UnitTests.Invoices;

public sealed class IssueInvoiceTests
{
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IDocumentSeriesRepository _series = Substitute.For<IDocumentSeriesRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    // 23:30 UTC on 31 March is already 1 April in Lisbon (summer time, UTC+1).
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 3, 31, 23, 30, 0, TimeSpan.Zero));

    private readonly IDocumentSigner _signer = Substitute.For<IDocumentSigner>();

    private IssueInvoice Handler => new(_invoices, _series, _unitOfWork, _signer, _clock);

    [Fact]
    public async Task Issues_with_todays_date_in_portugal()
    {
        var invoice = Draft();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _series.GetActiveAsync(DocumentType.Invoice, Arg.Any<CancellationToken>())
            .Returns(DocumentSeries.Create(DocumentType.Invoice, "2026A", "CSDF7T5H").Value);

        var result = await Handler.HandleAsync(invoice.Id, TestContext.Current.CancellationToken);

        result.Value.IssueDate.ShouldBe(new DateOnly(2026, 4, 1));
        result.Value.DocumentNumber.ShouldBe("FT 2026A/1");
        _signer.Received(1).Sign(Arg.Is<string>(m => m.StartsWith("2026-04-01;2026-04-01T00:30:00;FT 2026A/1;12.30;", StringComparison.Ordinal)));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_there_is_no_active_series()
    {
        var invoice = Draft();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await Handler.HandleAsync(invoice.Id, TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("Series.NoActiveSeries");
        invoice.IsDraft.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_the_document_does_not_exist()
    {
        var result = await Handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("Invoice.NotFound");
    }

    private static Invoice Draft()
    {
        var customer = Customer.Create("Padaria", "503504564", new Address("Rua 1", "4000-001", "Porto")).Value;
        var rate = TaxRate.Create(TaxRateCode.Normal, "Normal", 23m).Value;
        var product = Product.Create("P1", "Pão", ProductType.Goods, "UN", 1m, rate.Id).Value;
        var invoice = Invoice.CreateDraft(DocumentType.Invoice, customer).Value;
        invoice.AddLine(product, rate, 10, 1m);
        return invoice;
    }
}
