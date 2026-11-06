using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Domain.Series;

/// <summary>
/// A numbering series for one document type, e.g. <c>FT 2026A</c>. The series is the single source
/// of truth for the next number, so numbers are strictly sequential and never reused (see ADR 0003).
/// </summary>
public sealed class DocumentSeries : Entity
{
    private DocumentSeries(DocumentType documentType, string code, string validationCode)
    {
        DocumentType = documentType;
        Code = code;
        ValidationCode = validationCode;
        IsActive = true;
    }

    // Required by EF Core.
    private DocumentSeries()
    {
        Code = null!;
        ValidationCode = null!;
    }

    public DocumentType DocumentType { get; private set; }

    public string Code { get; private set; }

    /// <summary>
    /// Code AT assigns when a series is communicated. In this demo it is generated locally
    /// (see ADR 0004). It is the first half of every ATCUD.
    /// </summary>
    public string ValidationCode { get; private set; }

    public int LastNumber { get; private set; }

    public DateOnly? LastIssueDate { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Hash of the last issued document: the "previous hash" of the next one.</summary>
    public string? LastHash { get; private set; }

    public static Result<DocumentSeries> Create(DocumentType documentType, string code, string validationCode)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20 ||
            !code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            return SeriesErrors.InvalidCode;
        }

        if (string.IsNullOrWhiteSpace(validationCode) || validationCode.Length < 8)
        {
            return SeriesErrors.ValidationCodeRequired;
        }

        return new DocumentSeries(documentType, code.ToUpperInvariant(), validationCode.ToUpperInvariant());
    }

    public void Close() => IsActive = false;

    /// <summary>Reserves the next number for a document dated <paramref name="issueDate"/>.</summary>
    internal Result<int> ReserveNextNumber(DateOnly issueDate)
    {
        var error = CanIssue(issueDate);
        if (error is not null)
        {
            return error;
        }

        LastNumber++;
        LastIssueDate = issueDate;
        return LastNumber;
    }

    internal void RecordHash(string hash) => LastHash = hash;

    internal Error? CanIssue(DateOnly issueDate) =>
        !IsActive ? SeriesErrors.Inactive
        : issueDate < LastIssueDate ? SeriesErrors.DateBeforeLastIssued
        : null;

    /// <summary>Document number in the SAF-T format: <c>{type} {series}/{number}</c>, e.g. <c>FT 2026A/42</c>.</summary>
    public string FormatDocumentNumber(int number) => $"{DocumentType.ToCode()} {Code}/{number}";

    /// <summary>ATCUD: <c>{validation code}-{number}</c>, e.g. <c>CSDF7T5H-42</c>.</summary>
    public string FormatAtcud(int number) => $"{ValidationCode}-{number}";
}
