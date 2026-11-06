using InvoiceFlow.Domain.Taxes;

namespace InvoiceFlow.Domain.Invoices;

/// <summary>VAT totals for one rate, as printed in the summary box of an invoice.</summary>
public sealed record TaxSummaryLine(TaxRateCode Code, decimal Percentage, decimal TaxableAmount, decimal TaxAmount);
