using System.Security.Cryptography;
using InvoiceFlow.Domain.Customers;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Products;
using InvoiceFlow.Domain.Series;
using InvoiceFlow.Domain.Taxes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.Persistence;

/// <summary>Applies pending migrations and seeds reference and demo data on an empty database.</summary>
public sealed class DatabaseInitializer(AppDbContext db, TimeProvider timeProvider, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(bool seedDemoData, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.TaxRates.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding reference data (demo data: {SeedDemoData})", seedDemoData);

        var rates = SeedTaxRates();
        SeedSeries();

        if (seedDemoData)
        {
            SeedProducts(rates);
            SeedCustomers();
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private Dictionary<TaxRateCode, TaxRate> SeedTaxRates()
    {
        TaxRate[] rates =
        [
            TaxRate.Create(TaxRateCode.Normal, "Taxa normal", 23m).Value,
            TaxRate.Create(TaxRateCode.Intermediate, "Taxa intermédia", 13m).Value,
            TaxRate.Create(TaxRateCode.Reduced, "Taxa reduzida", 6m).Value,
            TaxRate.Create(TaxRateCode.Exempt, "Isento", 0m).Value,
        ];
        db.TaxRates.AddRange(rates);
        return rates.ToDictionary(r => r.Code);
    }

    private void SeedSeries()
    {
        var code = $"{timeProvider.GetUtcNow().Year}A";
        foreach (var type in Enum.GetValues<DocumentType>())
        {
            db.DocumentSeries.Add(DocumentSeries.Create(type, code, NewValidationCode()).Value);
        }
    }

    private void SeedProducts(Dictionary<TaxRateCode, TaxRate> rates)
    {
        (string Code, string Description, ProductType Type, string Unit, decimal Price, TaxRateCode Rate)[] products =
        [
            ("CONS-H", "Consultoria técnica", ProductType.Service, "H", 65m, TaxRateCode.Normal),
            ("DEV-H", "Desenvolvimento de software", ProductType.Service, "H", 55m, TaxRateCode.Normal),
            ("SUP-M", "Contrato de suporte mensal", ProductType.Service, "MES", 250m, TaxRateCode.Normal),
            ("FORM-H", "Formação profissional certificada", ProductType.Service, "H", 40m, TaxRateCode.Exempt),
            ("PORT-01", "Portátil 14\" 16GB RAM", ProductType.Goods, "UN", 899m, TaxRateCode.Normal),
            ("MON-27", "Monitor 27\" QHD", ProductType.Goods, "UN", 279.90m, TaxRateCode.Normal),
            ("CAFE-1K", "Café em grão 1 kg", ProductType.Goods, "KG", 18.50m, TaxRateCode.Intermediate),
            ("LIVRO-01", "Livro técnico", ProductType.Goods, "UN", 34.90m, TaxRateCode.Reduced),
        ];

        foreach (var p in products)
        {
            db.Products.Add(Product.Create(p.Code, p.Description, p.Type, p.Unit, p.Price, rates[p.Rate].Id).Value);
        }
    }

    private void SeedCustomers()
    {
        (string Name, string Nif, string Street, string PostalCode, string City, string? Email)[] customers =
        [
            ("Consumidor final", Nif.FinalConsumerValue, "Desconhecido", "0000-000", "Desconhecido", null),
            ("Padaria Central, Lda", "509123457", "Rua de Santa Catarina 120", "4000-447", "Porto", "geral@padariacentral.pt"),
            ("Atlântico Software, SA", "510234569", "Avenida da Liberdade 245", "1250-143", "Lisboa", "financeiro@atlantico.pt"),
            ("Quinta do Vale Verde, Lda", "513345671", "Estrada Nacional 222, km 14", "5110-204", "Armamar", null),
            ("Clínica Sorriso, Unipessoal Lda", "506456781", "Rua Ferreira Borges 85", "3000-180", "Coimbra", "contabilidade@clinicasorriso.pt"),
            ("Ferragens Minhotas, Lda", "514567899", "Rua do Souto 40", "4700-329", "Braga", null),
            ("Hotel Mar Azul, SA", "507678907", "Avenida 25 de Abril 10", "8125-401", "Vilamoura", "reservas@hotelmarazul.pt"),
            ("Ana Martins", "213456788", "Rua das Flores 7, 2.º Esq.", "2750-319", "Cascais", "ana.martins@example.pt"),
        ];

        foreach (var c in customers)
        {
            db.Customers.Add(Customer.Create(c.Name, c.Nif, new Address(c.Street, c.PostalCode, c.City), c.Email).Value);
        }
    }

    /// <summary>Stands in for the code AT returns when a series is communicated (ADR 0004).</summary>
    private static string NewValidationCode() =>
        RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 8);
}
