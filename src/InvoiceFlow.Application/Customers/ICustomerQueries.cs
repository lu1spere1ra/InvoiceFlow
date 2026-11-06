using FluentValidation;
using InvoiceFlow.Application.Common;

namespace InvoiceFlow.Application.Customers;

public sealed record CustomerListQuery : PageQuery
{
    /// <summary>Matches name or NIF.</summary>
    public string? Search { get; init; }
}

public interface ICustomerQueries
{
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CustomerDto>> ListAsync(CustomerListQuery query, CancellationToken cancellationToken);
}

public sealed class CustomerListQueryValidator : PageQueryValidator<CustomerListQuery>
{
    public CustomerListQueryValidator() => RuleFor(x => x.Search).MaximumLength(100);
}
