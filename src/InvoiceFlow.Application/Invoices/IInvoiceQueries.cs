using FluentValidation;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public enum InvoiceSort
{
    IssueDate,
    DueDate,
    Number,
    Customer,
    Total,
}

public sealed record InvoiceListQuery : PageQuery
{
    public InvoiceSort SortBy { get; init; } = InvoiceSort.IssueDate;

    public bool Descending { get; init; } = true;

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Guid? CustomerId { get; init; }

    /// <summary>Only issued invoices whose due date has passed.</summary>
    public bool OverdueOnly { get; init; }

    public DocumentType? Type { get; init; }

    public InvoiceStatus? Status { get; init; }

    /// <summary>Matches document number, customer name or NIF.</summary>
    public string? Search { get; init; }
}

public interface IInvoiceQueries
{
    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceSummaryDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken);
}

public sealed class InvoiceListQueryValidator : PageQueryValidator<InvoiceListQuery>
{
    public InvoiceListQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}
