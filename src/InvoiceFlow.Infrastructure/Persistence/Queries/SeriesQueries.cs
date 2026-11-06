using InvoiceFlow.Application.Series;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Queries;

internal sealed class SeriesQueries(AppDbContext db) : ISeriesQueries
{
    public async Task<IReadOnlyList<SeriesDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.DocumentSeries.AsNoTracking()
            .OrderByDescending(s => s.IsActive).ThenBy(s => s.DocumentType).ThenByDescending(s => s.Code)
            .Select(s => new SeriesDto(s.Id, s.DocumentType, s.Code, s.ValidationCode, s.LastNumber, s.LastIssueDate, s.IsActive))
            .ToListAsync(cancellationToken);
}
