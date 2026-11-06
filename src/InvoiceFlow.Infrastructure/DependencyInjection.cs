using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Infrastructure.Persistence.Queries;
using InvoiceFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseInitializer>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<IDocumentSeriesRepository, DocumentSeriesRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<IInvoiceQueries, InvoiceQueries>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();

        return services;
    }
}
