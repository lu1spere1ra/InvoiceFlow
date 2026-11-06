namespace InvoiceFlow.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes atomically.
    /// </summary>
    /// <exception cref="ConcurrencyConflictException">Another request changed the same data first.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Raised when an optimistic concurrency check or a uniqueness guarantee fails at save time, e.g. two
/// users issuing from the same series at the same instant. Mapped to HTTP 409; the client can retry.
/// </summary>
public sealed class ConcurrencyConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
