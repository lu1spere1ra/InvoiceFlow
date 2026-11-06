namespace InvoiceFlow.Domain.Invoices;

public enum DocumentType
{
    /// <summary>FT — Fatura.</summary>
    Invoice,

    /// <summary>FR — Fatura-recibo: invoice and receipt in one, paid on issue.</summary>
    InvoiceReceipt,

    /// <summary>NC — Nota de crédito, always referencing an issued invoice.</summary>
    CreditNote,
}

public static class DocumentTypeExtensions
{
    /// <summary>The AT/SAF-T (PT) <c>InvoiceType</c> code.</summary>
    public static string ToCode(this DocumentType type) => type switch
    {
        DocumentType.Invoice => "FT",
        DocumentType.InvoiceReceipt => "FR",
        DocumentType.CreditNote => "NC",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
