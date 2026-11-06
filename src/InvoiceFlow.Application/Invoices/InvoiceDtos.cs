using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Application.Invoices;

public sealed record InvoiceSummaryDto(
    Guid Id,
    DocumentType Type,
    InvoiceStatus Status,
    string? DocumentNumber,
    string CustomerName,
    string CustomerNif,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    decimal GrossTotal);

public sealed record InvoiceDto(
    Guid Id,
    DocumentType Type,
    InvoiceStatus Status,
    string? DocumentNumber,
    string? Atcud,
    Guid CustomerId,
    InvoiceCustomerDto Customer,
    int PaymentTermDays,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    DateTimeOffset? SystemEntryDate,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<TaxSummaryDto> TaxSummary,
    decimal NetTotal,
    decimal TaxTotal,
    decimal GrossTotal,
    DateOnly? PaidOn,
    string? VoidReason,
    Guid? OriginalInvoiceId,
    string? OriginalDocumentNumber,
    string? CreditReason,
    decimal CreditedAmount,
    string? HashExcerpt);

public sealed record InvoiceCustomerDto(string Name, string Nif, string Street, string PostalCode, string City, string Country);

public sealed record InvoiceLineDto(
    int LineNumber,
    Guid ProductId,
    string ProductCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    TaxRateCode TaxCode,
    decimal TaxPercentage,
    string? ExemptionCode,
    string? ExemptionReason,
    decimal NetAmount);

public sealed record TaxSummaryDto(TaxRateCode Code, decimal Percentage, decimal TaxableAmount, decimal TaxAmount);

public static class InvoiceMappings
{
    /// <param name="creditedAmount">Gross total of effective credit notes against this document.</param>
    public static InvoiceDto ToDto(this Invoice invoice, decimal creditedAmount = 0) => new(
        invoice.Id,
        invoice.Type,
        invoice.Status,
        invoice.DocumentNumber,
        invoice.Atcud,
        invoice.CustomerId,
        new InvoiceCustomerDto(
            invoice.Customer.Name,
            invoice.Customer.Nif,
            invoice.Customer.Address.Street,
            invoice.Customer.Address.PostalCode,
            invoice.Customer.Address.City,
            invoice.Customer.Address.Country),
        invoice.PaymentTermDays,
        invoice.IssueDate,
        invoice.DueDate,
        invoice.SystemEntryDate,
        invoice.Lines.Select(line => new InvoiceLineDto(
            line.LineNumber,
            line.ProductId,
            line.ProductCode,
            line.Description,
            line.Unit,
            line.Quantity,
            line.UnitPrice,
            line.DiscountPercent,
            line.TaxCode,
            line.TaxPercentage,
            line.ExemptionCode,
            line.ExemptionReason,
            line.NetAmount)).ToList(),
        invoice.TaxSummary.Select(tax => new TaxSummaryDto(tax.Code, tax.Percentage, tax.TaxableAmount, tax.TaxAmount)).ToList(),
        invoice.NetTotal,
        invoice.TaxTotal,
        invoice.GrossTotal,
        invoice.PaidOn,
        invoice.VoidReason,
        invoice.OriginalInvoiceId,
        invoice.OriginalDocumentNumber,
        invoice.CreditReason,
        creditedAmount,
        HashChain.Excerpt(invoice.Hash) is { Length: > 0 } excerpt ? excerpt : null);
}
