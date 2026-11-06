using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Invoices;

public static class InvoiceErrors
{
    public static readonly Error NotDraft = Error.Conflict(
        "Invoice.NotDraft", "Only draft documents can be changed. Issued documents are immutable.");

    public static readonly Error NoLines = new("Invoice.NoLines", "A document needs at least one line to be issued.");

    public static readonly Error QuantityMustBePositive = new("Invoice.QuantityMustBePositive", "The quantity must be greater than zero.");

    public static readonly Error NegativeUnitPrice = new("Invoice.NegativeUnitPrice", "The unit price cannot be negative.");

    public static readonly Error InvalidDiscount = new("Invoice.InvalidDiscount", "The discount must be between 0% and 100%.");

    public static readonly Error ExemptionReasonRequired = new(
        "Invoice.ExemptionReasonRequired", "Lines with an exempt VAT rate must state a valid exemption reason.");

    public static readonly Error ExemptionReasonNotAllowed = new(
        "Invoice.ExemptionReasonNotAllowed", "Only lines with an exempt VAT rate can have an exemption reason.");

    public static readonly Error InvalidPaymentTerms = new("Invoice.InvalidPaymentTerms", "Payment terms must be between 0 and 365 days.");

    public static readonly Error LineNotFound = Error.NotFound("Invoice.LineNotFound", "The line does not exist on this document.");

    public static readonly Error SeriesTypeMismatch = new(
        "Invoice.SeriesTypeMismatch", "The series belongs to a different document type.");

    public static readonly Error FinalConsumerLimitExceeded = new(
        "Invoice.FinalConsumerLimitExceeded",
        $"Documents for an anonymous final consumer cannot exceed {Invoice.FinalConsumerLimit:0.00} EUR. Identify the customer's NIF.");

    public static readonly Error NotIssued = Error.Conflict("Invoice.NotIssued", "Only issued documents can be paid or voided.");

    public static readonly Error OnlyInvoicesArePaid = new(
        "Invoice.OnlyInvoicesArePaid", "Only invoices (FT) are marked as paid. Invoice-receipts are paid when issued.");

    public static readonly Error PaymentInFuture = new("Invoice.PaymentInFuture", "The payment date cannot be in the future.");

    public static readonly Error PaymentBeforeIssue = new("Invoice.PaymentBeforeIssue", "The payment date cannot be earlier than the invoice date.");

    public static readonly Error VoidReasonRequired = new("Invoice.VoidReasonRequired", "State why the document is being voided.");

    public static readonly Error HasCreditNotes = Error.Conflict(
        "Invoice.HasCreditNotes", "This document has credit notes. Void the credit notes first or issue a new credit note.");

    public static readonly Error CreditNoteRequiresOriginal = new(
        "Invoice.CreditNoteRequiresOriginal", "Credit notes are created from an issued invoice.");

    public static readonly Error CreditNoteIsFixed = Error.Conflict(
        "Invoice.CreditNoteIsFixed", "Credit note lines come from the original invoice and cannot be edited. Delete the draft and create it again.");

    public static readonly Error NotCreditable = Error.Conflict(
        "Invoice.NotCreditable", "Only issued or paid invoices and invoice-receipts can be credited.");

    public static readonly Error CreditReasonRequired = new("Invoice.CreditReasonRequired", "State the reason for the credit note.");

    public static readonly Error CreditQuantityInvalid = new(
        "Invoice.CreditQuantityInvalid", "Credited quantities must be greater than zero and not exceed the original quantity.");

    public static readonly Error CreditExceedsRemaining = new(
        "Invoice.CreditExceedsRemaining", "The credit note exceeds the amount still creditable on the original invoice.");

    public static Error NotFound(Guid id) => Error.NotFound("Invoice.NotFound", $"Document '{id}' was not found.");
}
