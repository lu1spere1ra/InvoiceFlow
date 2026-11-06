using System.Diagnostics.CodeAnalysis;

namespace InvoiceFlow.Domain.Common;

/// <summary>
/// Outcome of an operation that can fail for an expected, business-level reason.
/// Exceptions are reserved for bugs and infrastructure failures (see ADR 0005).
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) => _value = default;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    // Implicit conversions keep call sites terse: `return customer;` or `return CustomerErrors.InvalidNif;`.
    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
