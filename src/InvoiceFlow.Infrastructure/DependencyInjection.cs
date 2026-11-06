using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Saft;
using InvoiceFlow.Application.Series;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Identity;
using InvoiceFlow.Infrastructure.Pdf;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Infrastructure.Persistence.Interceptors;
using InvoiceFlow.Infrastructure.Persistence.Queries;
using InvoiceFlow.Infrastructure.Persistence.Repositories;
using InvoiceFlow.Infrastructure.Saft;
using InvoiceFlow.Infrastructure.Signing;
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

        services.AddScoped<AuditingInterceptor>();
        services.AddSingleton<IssuedDocumentGuardInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(
                provider.GetRequiredService<AuditingInterceptor>(),
                provider.GetRequiredService<IssuedDocumentGuardInterceptor>()));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseInitializer>();
        services.AddIdentityAndJwt(configuration);

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();
        services.AddScoped<IDocumentSeriesRepository, DocumentSeriesRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<IInvoiceQueries, InvoiceQueries>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();
        services.AddScoped<ISeriesQueries, SeriesQueries>();

        services.AddSingleton(configuration.GetSection("Company").Get<CompanyProfile>() ?? new CompanyProfile());
        services.AddOptions<SigningOptions>().Bind(configuration.GetSection(SigningOptions.SectionName));
        services.AddSingleton<IDocumentSigner, RsaDocumentSigner>();
        services.AddSingleton<IAtSeriesRegistry, SimulatedAtSeriesRegistry>();
        services.AddSingleton<IInvoicePdfRenderer, QuestPdfInvoiceRenderer>();
        services.AddScoped<ISaftExporter, SaftPtExporter>();

        return services;
    }
}
