using InvoiceFlow.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Nif)
            .HasConversion(nif => nif.Value, value => Nif.Create(value).Value)
            .HasMaxLength(9)
            .IsUnicode(false);

        // Many anonymous "Consumidor final" records may exist; every other NIF is unique.
        builder.HasIndex(c => c.Nif).IsUnique().HasFilter($"[Nif] <> '{Nif.FinalConsumerValue}'");

        builder.ComplexProperty(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street");
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(2).IsFixedLength();
        });

        builder.HasIndex(c => c.Name);
    }
}
