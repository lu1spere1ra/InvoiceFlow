using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Application.Customers;

public sealed class CreateCustomer(ICustomerRepository customers, IUnitOfWork unitOfWork)
{
    public async Task<Result<CustomerDto>> HandleAsync(CustomerInput input, CancellationToken cancellationToken)
    {
        var address = new Address(input.Street, input.PostalCode, input.City, input.Country);
        var customer = Customer.Create(input.Name, input.Nif, address, input.Email);
        if (customer.IsFailure)
        {
            return customer.Error;
        }

        if (!customer.Value.Nif.IsFinalConsumer &&
            await customers.NifExistsAsync(customer.Value.Nif.Value, excludingCustomerId: null, cancellationToken))
        {
            return CustomerErrors.DuplicateNif;
        }

        customers.Add(customer.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.Value.ToDto();
    }
}
