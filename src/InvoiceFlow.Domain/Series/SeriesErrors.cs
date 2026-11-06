using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Series;

public static class SeriesErrors
{
    public static readonly Error InvalidCode = new(
        "Series.InvalidCode", "The series code must have 1 to 20 letters, digits, '-' or '_' (no spaces or '/').");

    public static readonly Error ValidationCodeRequired = new(
        "Series.ValidationCodeRequired", "The series needs an AT validation code (at least 8 characters) before issuing documents.");

    public static readonly Error Inactive = Error.Conflict("Series.Inactive", "The series is closed and cannot issue documents.");

    public static readonly Error DateBeforeLastIssued = Error.Conflict(
        "Series.DateBeforeLastIssued", "The document date cannot be earlier than the last document issued in the series.");

    public static readonly Error DuplicateCode = Error.Conflict("Series.DuplicateCode", "A series with this code already exists for the document type.");

    public static Error AlreadyActive(string documentType) =>
        Error.Conflict("Series.AlreadyActive", $"There is already an active {documentType} series. Close it before opening a new one.");

    public static Error NotFound(Guid id) => Error.NotFound("Series.NotFound", $"Series '{id}' was not found.");

    public static Error NoActiveSeries(string documentType) =>
        Error.Conflict("Series.NoActiveSeries", $"There is no active series for document type {documentType}.");
}
