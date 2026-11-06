namespace InvoiceFlow.Domain.Customers;

public sealed record Address(string Street, string PostalCode, string City, string Country = "PT");
