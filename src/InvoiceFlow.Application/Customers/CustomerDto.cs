using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Nif,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string? Email);

public static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Nif.Value,
        customer.Address.Street,
        customer.Address.PostalCode,
        customer.Address.City,
        customer.Address.Country,
        customer.Email);
}
