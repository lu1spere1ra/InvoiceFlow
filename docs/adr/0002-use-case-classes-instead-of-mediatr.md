# ADR 0002 — Plain use-case classes instead of MediatR

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

Many .NET Clean Architecture samples route every request through MediatR: the controller sends a
command object, and a handler is found by reflection. Pipeline behaviours add cross-cutting concerns
such as validation and logging.

Since 2025, MediatR (like AutoMapper and MassTransit) is distributed under a commercial licence for
most companies. Even ignoring the licence, the indirection has a cost: "go to definition" on
`mediator.Send(command)` leads nowhere useful, and the request → handler link only exists at run time.

## Decision

Each use case is a small, sealed class with a single `HandleAsync` method, registered explicitly in
`Application/DependencyInjection.cs` and injected straight into the controller action:

```csharp
[HttpPost("{id:guid}/issue")]
public async Task<ActionResult<InvoiceDto>> Issue(Guid id, [FromServices] IssueInvoice handler, CancellationToken ct)
```

- There is **no `ICommandHandler<T>` interface**. With no mediator and no decorators there is nothing
  that would consume it, so it would be abstraction for its own sake.
- **Validation** of request bodies runs in one MVC action filter (`ValidationFilter`) that looks up
  the FluentValidation validator for each action argument. That is the only cross-cutting concern that
  needed a pipeline.
- **Logging** of requests is done once by `UseSerilogRequestLogging`.
- **Reads** that only fetch data go through query interfaces (`IInvoiceQueries`) injected into the
  controller; wrapping them in a "query handler" would add a class that only forwards a call (ADR 0006).

## Consequences

- Navigation is direct: controller → use case → domain, all statically linked and refactor-safe.
- Adding a use case means one class and one line of registration.
- If more cross-cutting behaviour appears (e.g. auditing every command, transactions), an interface plus
  decorators can be introduced then. The cost of that change is small and paid only if needed.
- Controllers know about concrete use-case types. This is acceptable: they are the composition edge of
  the application and already depend on the Application layer.
