using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;
using InvoiceFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    public DbSet<DocumentSeries> DocumentSeries => Set<DocumentSeries>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as readable strings ("Issued", not 1): safer for reporting and refactoring.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }
}
