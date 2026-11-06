using FluentValidation;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.Series;

namespace InvoiceFlow.Application.Series;

public sealed record SeriesDto(
    Guid Id, DocumentType DocumentType, string Code, string ValidationCode, int LastNumber, DateOnly? LastIssueDate, bool IsActive);

public sealed record CreateSeriesInput(DocumentType DocumentType, string Code);

public sealed class CreateSeriesInputValidator : AbstractValidator<CreateSeriesInput>
{
    public CreateSeriesInputValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Use letters, digits, '-' or '_' only.");
    }
}

public sealed record SeriesVerificationDto(Guid SeriesId, int DocumentsChecked, bool IsValid, int? FirstBrokenNumber);

/// <summary>
/// Port for communicating a series to the tax authority. The real AT web service returns the validation
/// code used in every ATCUD; InvoiceFlow ships a local simulation (ADR 0004).
/// </summary>
public interface IAtSeriesRegistry
{
    Task<string> RegisterAsync(DocumentType documentType, string seriesCode, CancellationToken cancellationToken);
}

public interface ISeriesQueries
{
    Task<IReadOnlyList<SeriesDto>> ListAsync(CancellationToken cancellationToken);
}

public static class SeriesMappings
{
    public static SeriesDto ToDto(this DocumentSeries s) =>
        new(s.Id, s.DocumentType, s.Code, s.ValidationCode, s.LastNumber, s.LastIssueDate, s.IsActive);
}

/// <summary>Opens a new series. Only one series per document type can be active, so the current one must be closed first.</summary>
public sealed class CreateSeries(IDocumentSeriesRepository series, IAtSeriesRegistry registry, IUnitOfWork unitOfWork)
{
    public async Task<Result<SeriesDto>> HandleAsync(CreateSeriesInput input, CancellationToken cancellationToken)
    {
        if (await series.GetActiveAsync(input.DocumentType, cancellationToken) is not null)
        {
            return SeriesErrors.AlreadyActive(input.DocumentType.ToCode());
        }

        if (await series.ExistsAsync(input.DocumentType, input.Code.ToUpperInvariant(), cancellationToken))
        {
            return SeriesErrors.DuplicateCode;
        }

        var validationCode = await registry.RegisterAsync(input.DocumentType, input.Code, cancellationToken);
        var created = DocumentSeries.Create(input.DocumentType, input.Code, validationCode);
        if (created.IsFailure)
        {
            return created.Error;
        }

        series.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return created.Value.ToDto();
    }
}

public sealed class CloseSeries(IDocumentSeriesRepository series, IUnitOfWork unitOfWork)
{
    public async Task<Result<SeriesDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await series.GetByIdAsync(id, cancellationToken);
        if (found is null)
        {
            return SeriesErrors.NotFound(id);
        }

        found.Close();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return found.ToDto();
    }
}

/// <summary>Recomputes the hash chain of a series to detect tampering, deletions or gaps.</summary>
public sealed class VerifySeries(IDocumentSeriesRepository series, IInvoiceRepository invoices, IDocumentSigner signer)
{
    public async Task<Result<SeriesVerificationDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await series.GetByIdAsync(id, cancellationToken) is null)
        {
            return SeriesErrors.NotFound(id);
        }

        var documents = await invoices.ListIssuedInSeriesAsync(id, cancellationToken);
        var broken = HashChain.FindFirstBreak(documents, signer);
        return new SeriesVerificationDto(id, documents.Count, broken is null, broken);
    }
}
