using FluentValidation;
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

        services.AddScoped<InvoiceDraftWriter>();
        services.AddScoped<CreateInvoiceDraft>();
        services.AddScoped<UpdateInvoiceDraft>();
        services.AddScoped<DeleteInvoiceDraft>();
        services.AddScoped<IssueInvoice>();

        return services;
    }
}
