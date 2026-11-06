using InvoiceFlow.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The data was changed by another request. Please retry.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: UniqueConstraintViolation or UniqueIndexViolation })
        {
            throw new ConcurrencyConflictException("A record with the same unique value already exists.", exception);
        }
    }
}
