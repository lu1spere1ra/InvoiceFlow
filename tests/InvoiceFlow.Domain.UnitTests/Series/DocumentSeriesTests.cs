using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Series;

namespace InvoiceFlow.Domain.UnitTests.Series;

public sealed class DocumentSeriesTests
{
    [Fact]
    public void Numbers_are_sequential_without_gaps()
    {
        var series = TestData.Series();
        var invoices = Enumerable.Range(0, 5).Select(_ => TestData.DraftWithLine()).ToList();

        foreach (var invoice in invoices)
        {
            invoice.Issue(series, TestData.Today, TestData.Now);
        }

        invoices.Select(invoice => invoice.Number).ShouldBe([1, 2, 3, 4, 5]);
        series.LastNumber.ShouldBe(5);
    }

    [Fact]
    public void Document_dates_cannot_go_back_in_time_within_a_series()
    {
        var series = TestData.Series();
        TestData.DraftWithLine().Issue(series, TestData.Today, TestData.Now);

        var result = TestData.DraftWithLine().Issue(series, TestData.Today.AddDays(-1), TestData.Now);

        result.Error.ShouldBe(SeriesErrors.DateBeforeLastIssued);
        series.LastNumber.ShouldBe(1);
    }

    [Fact]
    public void Same_day_documents_are_allowed()
    {
        var series = TestData.Series();
        TestData.DraftWithLine().Issue(series, TestData.Today, TestData.Now);

        TestData.DraftWithLine().Issue(series, TestData.Today, TestData.Now).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026 A")]
    [InlineData("2026/A")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Rejects_invalid_codes(string code) =>
        DocumentSeries.Create(DocumentType.Invoice, code, "CSDF7T5H").Error.ShouldBe(SeriesErrors.InvalidCode);

    [Fact]
    public void Requires_a_validation_code() =>
        DocumentSeries.Create(DocumentType.Invoice, "2026A", "SHORT").Error.ShouldBe(SeriesErrors.ValidationCodeRequired);

    [Theory]
    [InlineData(DocumentType.Invoice, "FT 2026A/7")]
    [InlineData(DocumentType.InvoiceReceipt, "FR 2026A/7")]
    [InlineData(DocumentType.CreditNote, "NC 2026A/7")]
    public void Formats_document_numbers_like_saft(DocumentType type, string expected) =>
        TestData.Series(type).FormatDocumentNumber(7).ShouldBe(expected);
}
