using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Customers;

public static class CustomerErrors
{
    public static readonly Error InvalidNif = new("Customer.InvalidNif", "The NIF is not a valid Portuguese tax number.");
    public static readonly Error DuplicateNif = Error.Conflict("Customer.DuplicateNif", "Another customer already has this NIF.");
    public static readonly Error NameRequired = new("Customer.NameRequired", "The customer name is required.");

    public static Error NotFound(Guid id) => Error.NotFound("Customer.NotFound", $"Customer '{id}' was not found.");
}
