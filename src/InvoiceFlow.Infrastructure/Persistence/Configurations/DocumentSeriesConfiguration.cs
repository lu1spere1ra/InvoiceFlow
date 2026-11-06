using InvoiceFlow.Domain.Series;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class DocumentSeriesConfiguration : IEntityTypeConfiguration<DocumentSeries>
{
    public void Configure(EntityTypeBuilder<DocumentSeries> builder)
    {
        builder.Property(s => s.Code).HasMaxLength(20);
        builder.Property(s => s.ValidationCode).HasMaxLength(20);
        builder.HasIndex(s => new { s.DocumentType, s.Code }).IsUnique();

        // Optimistic concurrency token: two requests issuing from the same series at once cannot both
        // save, so a number is never handed out twice (ADR 0003). Kept out of the domain model.
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
