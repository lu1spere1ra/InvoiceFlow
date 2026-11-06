using InvoiceFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InvoiceFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Defence in depth for "issued documents are immutable". The aggregate already refuses changes; this
/// guard makes sure no code path (a bug, a future refactoring, a careless script using the DbContext)
/// can persist them. Only the lifecycle columns may change after issue.
/// </summary>
internal sealed class IssuedDocumentGuardInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> MutableAfterIssue =
    [
        nameof(Invoice.Status),
        nameof(Invoice.PaidOn),
        nameof(Invoice.VoidReason),
        nameof(Invoice.VoidedAt),
        ShadowProperties.UpdatedAt,
        ShadowProperties.UpdatedBy,
    ];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Guard(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Guard(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void Guard(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<Invoice>())
        {
            if (!WasIssued(entry))
            {
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                throw Violation(entry, "deleted");
            }

            if (entry.State == EntityState.Modified)
            {
                var changed = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name)
                    .Concat(entry.ComplexProperties.Where(p => p.IsModified).Select(p => p.Metadata.Name))
                    .Where(name => !MutableAfterIssue.Contains(name))
                    .ToList();

                if (changed.Count > 0)
                {
                    throw Violation(entry, $"changed ({string.Join(", ", changed)})");
                }
            }
        }

        foreach (var line in context.ChangeTracker.Entries<InvoiceLine>())
        {
            if (line.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var invoiceId = (Guid?)line.Property("InvoiceId").CurrentValue ?? (Guid?)line.Property("InvoiceId").OriginalValue;
            var owner = context.ChangeTracker.Entries<Invoice>().FirstOrDefault(e => e.Entity.Id == invoiceId);
            if (owner is not null && WasIssued(owner))
            {
                throw Violation(owner, "given different lines");
            }
        }
    }

    private static bool WasIssued(EntityEntry<Invoice> entry) =>
        entry.State != EntityState.Added &&
        entry.Property(i => i.Status).OriginalValue != InvoiceStatus.Draft;

    private static InvalidOperationException Violation(EntityEntry<Invoice> entry, string what) =>
        new($"Issued document {entry.Entity.DocumentNumber} cannot be {what}. Issued documents are immutable.");
}
