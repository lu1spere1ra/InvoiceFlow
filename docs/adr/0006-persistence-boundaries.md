# ADR 0006 — Persistence boundaries: repositories for writes, projections for reads

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

The Application layer must not depend on EF Core (ADR 0001), yet it needs to load aggregates, save them
atomically and serve list screens efficiently. There are three common options:

1. Expose `DbSet<T>` to the Application layer through an `IApplicationDbContext` interface.
2. Generic `IRepository<T>` with `GetAll()`, `Find(predicate)`, …
3. Small, specific repositories for aggregates, plus separate read queries.

Option 1 leaks EF Core (and `IQueryable` semantics) into use cases. Option 2 either leaks `IQueryable`
too or forces every list screen to load full aggregates.

## Decision

**Writes** go through one small repository per aggregate root (`IInvoiceRepository`,
`ICustomerRepository`, …) with only the methods the use cases need, returning tracked domain objects.
`IUnitOfWork.SaveChangesAsync` commits everything changed in the request in one transaction.

**Reads** go through query interfaces defined next to their feature (`IInvoiceQueries`,
`ICustomerQueries`, `ICatalogQueries`) and implemented in Infrastructure. List queries project straight
to DTOs with `Select`, so only the needed columns are read and invoice lines are never loaded for a list.
Controllers call these interfaces directly: wrapping them in a use-case class would only forward the call.

**Mapping is manual** (`ToDto()` extension methods). It is a few lines per type, refactor-safe and
debuggable, and avoids AutoMapper (now commercial) and its run-time surprises.

**Sorting and filtering** use a closed set of options (`InvoiceSort` enum, typed filters) mapped with
a `switch` to LINQ. Clients can never send expressions or column names that reach SQL.

**Cross-cutting persistence concerns live in EF Core interceptors**, not in the domain:

- `AuditingInterceptor` fills `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy` (shadow properties) from
  `ICurrentUser` and `TimeProvider`.
- Soft delete: entities marked with the empty `ISoftDeletable` interface (customers, products) are
  updated with `IsDeleted/DeletedAt` instead of deleted; a global query filter hides them. Unique indexes
  are filtered on `IsDeleted = 0` so a code or NIF can be reused after a delete.
- `IssuedDocumentGuardInterceptor` refuses to save any change to an issued document except its
  lifecycle columns (status, payment, void), as defence in depth behind the aggregate's own rules.

**The domain model is mapped directly** (private setters, backing fields, complex types for value
objects such as `Address` and `CustomerSnapshot`), with no separate persistence model.

## Consequences

- Use cases are unit-testable with NSubstitute fakes of narrow interfaces.
- List screens stay fast as data grows; detail views reuse the aggregate mapping because they need
  computed values (VAT summary).
- Audit and soft-delete columns are invisible to the domain; tests read them with `EF.Property`.
- Bulk operations (`ExecuteUpdate`) bypass interceptors. They are used only for refresh tokens, which
  are neither audited nor protected documents.
