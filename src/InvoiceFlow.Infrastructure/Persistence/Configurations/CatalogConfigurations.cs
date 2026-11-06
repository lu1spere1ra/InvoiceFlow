using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Taxes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.Property(t => t.Percentage).HasPrecision(5, 2);
        builder.HasIndex(t => t.Code).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(60);
        builder.Property(p => p.Unit).HasMaxLength(20);
        builder.Property(p => p.UnitPrice).HasPrecision(18, 4);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasOne<TaxRate>().WithMany().HasForeignKey(p => p.TaxRateId).OnDelete(DeleteBehavior.Restrict);
    }
}
