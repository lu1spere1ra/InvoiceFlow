using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Domain.Invoices;

/// <summary>
/// Copy of the customer data as it was when the document was written. An issued invoice must keep
/// showing the original name and address even if the customer record changes later.
/// </summary>
public sealed record CustomerSnapshot(string Name, string Nif, Address Address)
{
    public static CustomerSnapshot From(Customer customer) => new(customer.Name, customer.Nif.Value, customer.Address);

    public bool IsFinalConsumer => Nif == Customers.Nif.FinalConsumerValue;
}
