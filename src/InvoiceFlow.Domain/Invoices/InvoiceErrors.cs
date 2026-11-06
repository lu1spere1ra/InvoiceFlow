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

    public static Error NotFound(Guid id) => Error.NotFound("Invoice.NotFound", $"Document '{id}' was not found.");
}
