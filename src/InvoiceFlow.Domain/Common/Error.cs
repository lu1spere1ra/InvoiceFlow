namespace InvoiceFlow.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>
/// An expected business failure. Codes are stable identifiers (e.g. <c>Invoice.NotDraft</c>) that
/// clients can rely on; messages are human-readable.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
