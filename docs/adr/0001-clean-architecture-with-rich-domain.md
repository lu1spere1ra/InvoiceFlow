# ADR 0001 — Clean Architecture with a rich domain model

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

InvoiceFlow handles fiscal documents whose rules are strict and stable: totals must add up, issued
documents can never change, numbering must be sequential without gaps, credit notes cannot exceed
the original. These rules are the valuable part of the system; the database, the web framework and
the PDF library are details that should be replaceable and should not leak into the rules.

## Decision

Split the backend into four projects with a one-way dependency rule:

```
Api ──► Infrastructure ──► Application ──► Domain
```

| Project | Owns | Must not know about |
|---|---|---|
| **Domain** | Entities, value objects, invariants, state transitions, domain errors | Anything outside the BCL |
| **Application** | Use cases, ports (interfaces such as repositories and the signer), validators, DTOs | EF Core, ASP.NET Core, HTTP |
| **Infrastructure** | EF Core `DbContext`, repositories, Identity/JWT, QuestPDF, SAF-T writer, RSA signing | Controllers |
| **Api** | Controllers, auth configuration, ProblemDetails mapping, OpenAPI, composition root | — |

Entities are **not anemic**: they have no public setters, are created through factory methods that
validate input, and expose behaviour (`invoice.AddLine(...)`, `invoice.Issue(...)`, `invoice.Void(...)`)
instead of data bags. If a rule can be checked with the data the aggregate holds, it lives in the aggregate.

The rule is enforced automatically by `tests/InvoiceFlow.Architecture.Tests`, which inspects assembly
references with plain reflection (no extra library needed).

## Consequences

- Domain logic is unit-testable in milliseconds with no mocks or database.
- Swapping SQL Server or the PDF library touches only Infrastructure.
- More projects and some mapping code than a single-project API. For a domain this rule-heavy the
  trade-off is worth it; for a CRUD app it would not be.
- EF Core maps the domain entities directly (private setters and backing fields), instead of using a
  separate persistence model. This is a pragmatic compromise: a few concessions such as a private
  parameterless constructor in exchange for not maintaining two parallel models.
