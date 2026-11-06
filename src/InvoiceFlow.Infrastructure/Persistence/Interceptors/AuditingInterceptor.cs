using InvoiceFlow.Application.Auth;
using InvoiceFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InvoiceFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps CreatedAt/CreatedBy and UpdatedAt/UpdatedBy on every entity, and turns deletes of
/// <see cref="ISoftDeletable"/> entities into updates of IsDeleted/DeletedAt.
/// </summary>
internal sealed class AuditingInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private const string SystemUser = "system";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Stamp(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext context)
    {
        var now = timeProvider.GetUtcNow();
        var user = currentUser.Email ?? SystemUser;

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable })
            {
                entry.State = EntityState.Modified;
                entry.Property(ShadowProperties.IsDeleted).CurrentValue = true;
                entry.Property(ShadowProperties.DeletedAt).CurrentValue = now;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(ShadowProperties.CreatedAt).CurrentValue = now;
                    entry.Property(ShadowProperties.CreatedBy).CurrentValue = user;
                    break;
                case EntityState.Modified:
                    entry.Property(ShadowProperties.UpdatedAt).CurrentValue = now;
                    entry.Property(ShadowProperties.UpdatedBy).CurrentValue = user;
                    break;
            }
        }
    }
}
