using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Domain.UnitTests.Invoices;

public sealed class CreditNoteTests
{
    // Original: line 1 = 10 × 50 € (23%), line 2 = 2 × 30 € (exempt M07). Gross 615 + 60 = 675 €.
    private static Invoice IssuedOriginal()
    {
        var invoice = TestData.Draft();
        invoice.AddLine(TestData.Product(), TestData.Normal, 10, 50m);
        invoice.AddLine(TestData.Product(), TestData.Exempt, 2, 30m, exemptionCode: "M07");
        invoice.Issue(TestData.Series(), TestData.Today, TestData.Now, TestData.Signer);
        return invoice;
    }

    [Fact]
    public void Copies_lines_and_references_the_original()
    {
        var original = IssuedOriginal();

        var note = Invoice.CreateCreditNote(original, "Returned goods", [new CreditNoteLine(1, 4), new CreditNoteLine(2, 2)], 0).Value;

        note.Type.ShouldBe(DocumentType.CreditNote);
        note.Status.ShouldBe(InvoiceStatus.Draft);
        note.OriginalInvoiceId.ShouldBe(original.Id);
        note.OriginalDocumentNumber.ShouldBe("FT 2026A/1");
        note.CreditReason.ShouldBe("Returned goods");
        note.Customer.ShouldBe(original.Customer);
        note.Lines.Select(l => (l.LineNumber, l.Quantity, l.UnitPrice)).ShouldBe([(1, 4m, 50m), (2, 2m, 30m)]);
        note.Lines[1].ExemptionCode.ShouldBe("M07");
        note.GrossTotal.ShouldBe(306m); // 200 + 46 VAT + 60 exempt
    }

    [Fact]
    public void Cannot_credit_more_than_the_original_quantity() =>
        Invoice.CreateCreditNote(IssuedOriginal(), "x", [new CreditNoteLine(1, 11)], 0)
            .Error.ShouldBe(InvoiceErrors.CreditQuantityInvalid);

    [Fact]
    public void Cannot_credit_more_than_what_remains()
    {
        var original = IssuedOriginal();

        // 615 € already credited leaves exactly 60 €: crediting line 2 in full fits...
        Invoice.CreateCreditNote(original, "x", [new CreditNoteLine(2, 2)], alreadyCredited: 615m).IsSuccess.ShouldBeTrue();

        // ...but one more cent already credited and it no longer fits.
        Invoice.CreateCreditNote(original, "x", [new CreditNoteLine(2, 2)], alreadyCredited: 615.01m)
            .Error.ShouldBe(InvoiceErrors.CreditExceedsRemaining);
    }

    [Fact]
    public void Only_effective_invoices_can_be_credited()
    {
        Invoice.CreateCreditNote(TestData.DraftWithLine(), "x", [new CreditNoteLine(1, 1)], 0).Error.ShouldBe(InvoiceErrors.NotCreditable);

        var voided = IssuedOriginal();
        voided.Void("wrong customer", TestData.Now);
        Invoice.CreateCreditNote(voided, "x", [new CreditNoteLine(1, 1)], 0).Error.ShouldBe(InvoiceErrors.NotCreditable);
    }

    [Fact]
    public void Requires_reason_and_known_lines()
    {
        var original = IssuedOriginal();

        Invoice.CreateCreditNote(original, " ", [new CreditNoteLine(1, 1)], 0).Error.ShouldBe(InvoiceErrors.CreditReasonRequired);
        Invoice.CreateCreditNote(original, "x", [], 0).Error.ShouldBe(InvoiceErrors.NoLines);
        Invoice.CreateCreditNote(original, "x", [new CreditNoteLine(9, 1)], 0).Error.ShouldBe(InvoiceErrors.LineNotFound);
    }

    [Fact]
    public void Credit_note_lines_cannot_be_edited_and_it_issues_in_its_own_series()
    {
        var note = Invoice.CreateCreditNote(IssuedOriginal(), "x", [new CreditNoteLine(1, 1)], 0).Value;

        note.AddLine(TestData.Product(), TestData.Normal, 1, 1m).Error.ShouldBe(InvoiceErrors.CreditNoteIsFixed);
        note.ClearLines().Error.ShouldBe(InvoiceErrors.CreditNoteIsFixed);

        note.Issue(TestData.Series(DocumentType.CreditNote), TestData.Today, TestData.Now, TestData.Signer).IsSuccess.ShouldBeTrue();
        note.DocumentNumber.ShouldBe("NC 2026A/1");
        note.Status.ShouldBe(InvoiceStatus.Issued);
    }
}
