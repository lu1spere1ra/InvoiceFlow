namespace InvoiceFlow.Domain.Common;

public abstract class Entity
{
    // Version 7 GUIDs are time-ordered, which keeps SQL Server clustered indexes from fragmenting.
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
