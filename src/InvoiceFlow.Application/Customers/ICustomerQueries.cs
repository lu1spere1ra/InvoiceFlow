using FluentValidation;
using InvoiceFlow.Application.Common;

namespace InvoiceFlow.Application.Customers;

public enum CustomerSort
{
    Name,
    Nif,
    City,
}

public sealed record CustomerListQuery : PageQuery
{
    public CustomerSort SortBy { get; init; } = CustomerSort.Name;

    public bool Descending { get; init; }

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
    public CustomerListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).IsInEnum();
    }
}
