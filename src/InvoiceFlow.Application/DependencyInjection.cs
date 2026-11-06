using FluentValidation;
using InvoiceFlow.Application.Catalog;
using InvoiceFlow.Application.Customers;
using InvoiceFlow.Application.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers use cases explicitly. A list is easier to read and debug than assembly scanning,
    /// and adding a use case is a one-line change (see ADR 0002).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CustomerInputValidator>(includeInternalTypes: true);

        services.AddScoped<CreateCustomer>();
        services.AddScoped<UpdateCustomer>();
        services.AddScoped<DeleteCustomer>();

        services.AddScoped<CreateProduct>();
        services.AddScoped<UpdateProduct>();
        services.AddScoped<DeleteProduct>();

        services.AddScoped<InvoiceDraftWriter>();
        services.AddScoped<CreateInvoiceDraft>();
        services.AddScoped<UpdateInvoiceDraft>();
        services.AddScoped<DeleteInvoiceDraft>();
        services.AddScoped<IssueInvoice>();
        services.AddScoped<MarkInvoicePaid>();
        services.AddScoped<VoidInvoice>();
        services.AddScoped<CreateCreditNote>();

        return services;
    }
}
