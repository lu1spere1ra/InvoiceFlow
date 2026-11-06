using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Customers;

namespace InvoiceFlow.Application.Customers;

/// <summary>
/// Changes the customer record. Issued documents are unaffected: they keep their own snapshot.
/// </summary>
public sealed class UpdateCustomer(ICustomerRepository customers, IUnitOfWork unitOfWork)
{
    public async Task<Result<CustomerDto>> HandleAsync(Guid id, CustomerInput input, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        var address = new Address(input.Street, input.PostalCode, input.City, input.Country);
        var updated = customer.Update(input.Name, input.Nif, address, input.Email);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        if (!customer.Nif.IsFinalConsumer && await customers.NifExistsAsync(customer.Nif.Value, id, cancellationToken))
        {
            return CustomerErrors.DuplicateNif;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }
}

public sealed class DeleteCustomer(ICustomerRepository customers, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        customers.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
