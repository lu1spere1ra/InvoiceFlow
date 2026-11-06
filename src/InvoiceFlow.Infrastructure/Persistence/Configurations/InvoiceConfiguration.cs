using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Series;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ComplexProperty(i => i.Customer, customer =>
        {
            customer.Property(c => c.Name).HasColumnName("CustomerName");
            customer.Property(c => c.Nif).HasColumnName("CustomerNif").HasMaxLength(9).IsUnicode(false);
            customer.ComplexProperty(c => c.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("CustomerStreet");
                address.Property(a => a.PostalCode).HasColumnName("CustomerPostalCode").HasMaxLength(20);
                address.Property(a => a.City).HasColumnName("CustomerCity").HasMaxLength(100);
                address.Property(a => a.Country).HasColumnName("CustomerCountry").HasMaxLength(2).IsFixedLength();
            });
        });

        builder.HasOne<Customer>().WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentSeries>().WithMany().HasForeignKey(i => i.SeriesId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.DocumentNumber).HasMaxLength(60);
        builder.Property(i => i.Atcud).HasMaxLength(100);
        builder.Property(i => i.Hash).HasMaxLength(172).IsUnicode(false); // SAF-T limit
        builder.Property(i => i.HashControl).HasMaxLength(70);
        builder.Property(i => i.OriginalDocumentNumber).HasMaxLength(60);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(i => i.OriginalInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.OriginalInvoiceId).HasFilter("[OriginalInvoiceId] IS NOT NULL");

        // Last line of defence for numbering: even a bug elsewhere cannot persist a duplicate number.
        builder.HasIndex(i => new { i.SeriesId, i.Number }).IsUnique().HasFilter("[Number] IS NOT NULL");
        builder.HasIndex(i => i.DocumentNumber).IsUnique().HasFilter("[DocumentNumber] IS NOT NULL");
        builder.HasIndex(i => new { i.Status, i.IssueDate });

        builder.OwnsMany(i => i.Lines, line =>
        {
            line.ToTable("InvoiceLines");
            line.WithOwner().HasForeignKey("InvoiceId");
            line.Property<int>("Id");
            line.HasKey("Id");

            line.Property(l => l.ProductCode).HasMaxLength(60);
            line.Property(l => l.Unit).HasMaxLength(20);
            line.Property(l => l.Quantity).HasPrecision(18, 4);
            line.Property(l => l.UnitPrice).HasPrecision(18, 4);
            line.Property(l => l.DiscountPercent).HasPrecision(5, 2);
            line.Property(l => l.TaxPercentage).HasPrecision(5, 2);
            line.Property(l => l.ExemptionCode).HasMaxLength(3);
        });

        builder.Ignore(i => i.TaxSummary); // computed from the lines

        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
