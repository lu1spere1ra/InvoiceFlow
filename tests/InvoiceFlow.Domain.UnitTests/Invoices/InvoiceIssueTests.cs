using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Series;

namespace InvoiceFlow.Domain.UnitTests.Invoices;

public sealed class InvoiceIssueTests
{
    [Fact]
    public void Issuing_assigns_number_atcud_dates_and_status()
    {
        var invoice = TestData.DraftWithLine();
        var series = TestData.Series();

        var result = invoice.Issue(series, TestData.Today, TestData.Now);

        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Issued);
        invoice.Number.ShouldBe(1);
        invoice.DocumentNumber.ShouldBe("FT 2026A/1");
        invoice.Atcud.ShouldBe("CSDF7T5H-1");
        invoice.IssueDate.ShouldBe(TestData.Today);
        invoice.DueDate.ShouldBe(TestData.Today.AddDays(30));
        invoice.SystemEntryDate.ShouldBe(TestData.Now);
        invoice.SeriesId.ShouldBe(series.Id);
    }

    [Fact]
    public void Issued_documents_are_immutable()
    {
        var invoice = TestData.DraftWithLine();
        invoice.Issue(TestData.Series(), TestData.Today, TestData.Now);

        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 1m).Error.ShouldBe(InvoiceErrors.NotDraft);
        invoice.RemoveLine(1).Error.ShouldBe(InvoiceErrors.NotDraft);
        invoice.ClearLines().Error.ShouldBe(InvoiceErrors.NotDraft);
        invoice.ChangeCustomer(TestData.Customer()).Error.ShouldBe(InvoiceErrors.NotDraft);
        invoice.ChangePaymentTerms(10).Error.ShouldBe(InvoiceErrors.NotDraft);
        invoice.GrossTotal.ShouldBe(123m);
    }

    [Fact]
    public void A_document_cannot_be_issued_twice()
    {
        var invoice = TestData.DraftWithLine();
        var series = TestData.Series();
        invoice.Issue(series, TestData.Today, TestData.Now);

        invoice.Issue(series, TestData.Today, TestData.Now).Error.ShouldBe(InvoiceErrors.NotDraft);
        series.LastNumber.ShouldBe(1);
    }

    [Fact]
    public void An_empty_draft_cannot_be_issued() =>
        TestData.Draft().Issue(TestData.Series(), TestData.Today, TestData.Now).Error.ShouldBe(InvoiceErrors.NoLines);

    [Fact]
    public void The_series_must_match_the_document_type() =>
        TestData.DraftWithLine().Issue(TestData.Series(DocumentType.CreditNote), TestData.Today, TestData.Now)
            .Error.ShouldBe(InvoiceErrors.SeriesTypeMismatch);

    [Fact]
    public void Final_consumer_documents_are_limited_to_1000_euros()
    {
        var atLimit = TestData.DraftWithLine(unitPrice: 813.00m, customer: TestData.FinalConsumer()); // 999.99
        var overLimit = TestData.DraftWithLine(unitPrice: 813.02m, customer: TestData.FinalConsumer()); // 1000.01
        var series = TestData.Series();

        atLimit.Issue(series, TestData.Today, TestData.Now).IsSuccess.ShouldBeTrue();
        overLimit.Issue(series, TestData.Today, TestData.Now).Error.ShouldBe(InvoiceErrors.FinalConsumerLimitExceeded);
    }

    [Fact]
    public void A_failed_issue_does_not_consume_a_number()
    {
        var series = TestData.Series();

        TestData.Draft().Issue(series, TestData.Today, TestData.Now).IsFailure.ShouldBeTrue();

        series.LastNumber.ShouldBe(0);
    }

    [Fact]
    public void Customer_snapshot_is_frozen_at_issue()
    {
        var customer = TestData.Customer();
        var invoice = TestData.DraftWithLine(customer: customer);
        invoice.Issue(TestData.Series(), TestData.Today, TestData.Now);

        customer.Update("New Name, SA", "503504564", customer.Address, null);

        invoice.Customer.Name.ShouldBe("Padaria Central, Lda");
    }

    [Fact]
    public void Closed_series_cannot_issue()
    {
        var series = TestData.Series();
        series.Close();

        TestData.DraftWithLine().Issue(series, TestData.Today, TestData.Now).Error.ShouldBe(SeriesErrors.Inactive);
    }
}
