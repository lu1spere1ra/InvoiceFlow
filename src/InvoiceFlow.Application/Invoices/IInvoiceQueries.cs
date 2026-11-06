using FluentValidation;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed record InvoiceListQuery : PageQuery
{
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
    }
}
