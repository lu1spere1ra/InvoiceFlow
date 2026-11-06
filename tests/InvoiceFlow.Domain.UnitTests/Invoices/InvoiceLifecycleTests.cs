using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Domain.UnitTests.Invoices;

public sealed class InvoiceLifecycleTests
{
    private static Invoice Issued(DocumentType type = DocumentType.Invoice)
    {
        var invoice = Invoice.CreateDraft(type, TestData.Customer()).Value;
        invoice.AddLine(TestData.Product(), TestData.Normal, 1, 100m);
        invoice.Issue(TestData.Series(type), TestData.Today, TestData.Now).IsSuccess.ShouldBeTrue();
        return invoice;
    }

    [Fact]
    public void Invoice_receipts_are_paid_on_issue()
    {
        var receipt = Issued(DocumentType.InvoiceReceipt);

        receipt.Status.ShouldBe(InvoiceStatus.Paid);
        receipt.PaidOn.ShouldBe(TestData.Today);
        receipt.DueDate.ShouldBeNull();
        receipt.DocumentNumber.ShouldBe("FR 2026A/1");
    }

    [Fact]
    public void Issued_invoices_can_be_marked_as_paid()
    {
        var invoice = Issued();

        invoice.MarkAsPaid(TestData.Today.AddDays(12)).IsSuccess.ShouldBeTrue();

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.PaidOn.ShouldBe(TestData.Today.AddDays(12));
    }

    [Fact]
    public void Payment_rules()
    {
        TestData.DraftWithLine().MarkAsPaid(TestData.Today).Error.ShouldBe(InvoiceErrors.NotIssued);
        Issued().MarkAsPaid(TestData.Today.AddDays(-1)).Error.ShouldBe(InvoiceErrors.PaymentBeforeIssue);
        Issued(DocumentType.InvoiceReceipt).MarkAsPaid(TestData.Today).Error.ShouldBe(InvoiceErrors.OnlyInvoicesArePaid);

        var paid = Issued();
        paid.MarkAsPaid(TestData.Today);
        paid.MarkAsPaid(TestData.Today).Error.ShouldBe(InvoiceErrors.NotIssued);
    }

    [Fact]
    public void Voiding_keeps_the_number_and_records_the_reason()
    {
        var invoice = Issued();

        invoice.Void("Duplicated by mistake", TestData.Now).IsSuccess.ShouldBeTrue();

        invoice.Status.ShouldBe(InvoiceStatus.Voided);
        invoice.DocumentNumber.ShouldBe("FT 2026A/1");
        invoice.VoidReason.ShouldBe("Duplicated by mistake");
        invoice.IsEffective.ShouldBeFalse();
    }

    [Fact]
    public void Void_rules()
    {
        TestData.DraftWithLine().Void("x", TestData.Now).Error.ShouldBe(InvoiceErrors.NotIssued);
        Issued().Void(" ", TestData.Now).Error.ShouldBe(InvoiceErrors.VoidReasonRequired);
        Issued().Void("x", TestData.Now, creditedAmount: 10m).Error.ShouldBe(InvoiceErrors.HasCreditNotes);

        var voided = Issued();
        voided.Void("x", TestData.Now);
        voided.Void("again", TestData.Now).Error.ShouldBe(InvoiceErrors.NotIssued);
        voided.MarkAsPaid(TestData.Today).Error.ShouldBe(InvoiceErrors.NotIssued);
    }

    [Fact]
    public void Paid_invoices_can_still_be_voided() =>
        Issued(DocumentType.InvoiceReceipt).Void("Customer returned everything", TestData.Now).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Credit_notes_cannot_be_created_as_plain_drafts() =>
        Invoice.CreateDraft(DocumentType.CreditNote, TestData.Customer()).Error.ShouldBe(InvoiceErrors.CreditNoteRequiresOriginal);
}
