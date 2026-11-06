# ADR 0005 — Expected errors as results, mapped to ProblemDetails

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

Most failures in an invoicing system are expected business outcomes, not bugs: an invalid NIF, issuing a
document twice, an exempt line without an exemption reason. Throwing exceptions for these makes control
flow hard to follow, hides the possible failures from method signatures and costs a stack trace each time.

## Decision

- Domain methods and use cases return `Result` / `Result<T>`. A failure carries an `Error` with a stable
  **code** (`Invoice.NotDraft`), a human-readable **message** and a **type** (`Validation`, `NotFound`,
  `Conflict`).
- Errors are declared once per aggregate (`InvoiceErrors`, `CustomerErrors`, …) so the possible
  failures of a feature are listed in one place.
- The API maps every error to an RFC 9457 `ProblemDetails` response, adding the code as an extension
  member so clients can branch on it without parsing text:

| Situation | Status |
|---|---|
| Malformed request (FluentValidation) | 400 with `errors` per field |
| `ErrorType.NotFound` | 404 |
| `ErrorType.Conflict` (wrong state, duplicate) or a concurrency conflict at save time | 409 |
| `ErrorType.Validation` (business rule broken) | 422 |
| Anything unexpected | 500, details logged, not leaked |

- **Two layers of validation, on purpose.** FluentValidation checks the *shape* of requests (required
  fields, lengths, formats) and gives friendly field-level messages. The domain enforces *invariants*
  (NIF check digit, state transitions, totals) no matter how it is called. Some rules appear in both;
  the domain is the one that cannot be bypassed.
- Exceptions remain for programming errors and infrastructure failures. The one exception translated to
  a client error is `ConcurrencyConflictException`, raised by the unit of work when the database rejects
  a save (ADR 0003).

## Consequences

- Every failure path is visible in the signature and covered by unit tests without try/catch.
- A small amount of `if (result.IsFailure) return result.Error;` boilerplate. No functional-programming
  library was added to hide it; the explicit version is easier to read for most .NET teams.
