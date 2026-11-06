namespace InvoiceFlow.Domain.Invoices;

/// <summary>A line of the original invoice to credit, and how much of it.</summary>
public sealed record CreditNoteLine(int LineNumber, decimal Quantity);
