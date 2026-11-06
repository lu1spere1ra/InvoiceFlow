using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Customers;

public sealed class Customer : Entity, ISoftDeletable
{
    private Customer(string name, Nif nif, Address address, string? email)
    {
        Name = name;
        Nif = nif;
        Address = address;
        Email = email;
    }

    // Required by EF Core.
    private Customer()
    {
        Name = null!;
        Nif = null!;
        Address = null!;
    }

    public string Name { get; private set; }

    public Nif Nif { get; private set; }

    public Address Address { get; private set; }

    public string? Email { get; private set; }

    public static Result<Customer> Create(string name, string nif, Address address, string? email = null)
    {
        var validNif = Nif.Create(nif);
        if (validNif.IsFailure)
        {
            return validNif.Error;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        return new Customer(name.Trim(), validNif.Value, address, NormalizeEmail(email));
    }

    public Result Update(string name, string nif, Address address, string? email)
    {
        var validNif = Nif.Create(nif);
        if (validNif.IsFailure)
        {
            return validNif.Error;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        Name = name.Trim();
        Nif = validNif.Value;
        Address = address;
        Email = NormalizeEmail(email);
        return Result.Success();
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();
}
