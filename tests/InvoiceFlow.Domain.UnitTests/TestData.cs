using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Domain.UnitTests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 3, 10);
    public static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    public static TaxRate Normal { get; } = TaxRate.Create(TaxRateCode.Normal, "Taxa normal", 23m).Value;
    public static TaxRate Intermediate { get; } = TaxRate.Create(TaxRateCode.Intermediate, "Taxa intermédia", 13m).Value;
    public static TaxRate Reduced { get; } = TaxRate.Create(TaxRateCode.Reduced, "Taxa reduzida", 6m).Value;
    public static TaxRate Exempt { get; } = TaxRate.Create(TaxRateCode.Exempt, "Isento", 0m).Value;

    public static Customer Customer(string nif = "503504564") =>
        Domain.Customers.Customer.Create("Padaria Central, Lda", nif, new Address("Rua Direita 1", "4000-001", "Porto")).Value;

    public static Customer FinalConsumer() => Customer(Nif.FinalConsumerValue);

    public static Product Product(decimal price = 10m) =>
        Domain.Products.Product.Create("CONS-01", "Consultoria", ProductType.Service, "H", price, Normal.Id).Value;

    public static DocumentSeries Series(DocumentType type = DocumentType.Invoice) =>
        DocumentSeries.Create(type, "2026A", "CSDF7T5H").Value;

    public static Invoice Draft(Customer? customer = null) =>
        Invoice.CreateDraft(DocumentType.Invoice, customer ?? Customer()).Value;

    public static Invoice DraftWithLine(decimal quantity = 1, decimal unitPrice = 100m, Customer? customer = null)
    {
        var invoice = Draft(customer);
        invoice.AddLine(Product(), Normal, quantity, unitPrice).IsSuccess.ShouldBeTrue();
        return invoice;
    }
}
