using FluentValidation;
using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Application.Saft;

public sealed record SaftPeriod(DateOnly From, DateOnly To);

/// <summary>A SAF-T file covers a period within one fiscal year (here: the calendar year).</summary>
public sealed class SaftPeriodValidator : AbstractValidator<SaftPeriod>
{
    public SaftPeriodValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end date must not be before the start date.");
        RuleFor(x => x.To.Year).Equal(x => x.From.Year).WithName("To").WithMessage("The period must be within one fiscal year.");
    }
}

public sealed record SaftFile(string FileName, byte[] Content);

/// <summary>Writes the SAF-T (PT) 1.04_01 audit file for invoicing software (TaxAccountingBasis "F").</summary>
public interface ISaftExporter
{
    Task<byte[]> ExportAsync(SaftPeriod period, CancellationToken cancellationToken);
}

public sealed class ExportSaft(ISaftExporter exporter)
{
    public async Task<Result<SaftFile>> HandleAsync(SaftPeriod period, CancellationToken cancellationToken)
    {
        var content = await exporter.ExportAsync(period, cancellationToken);
        return new SaftFile($"SAFT-PT_{period.From:yyyyMMdd}_{period.To:yyyyMMdd}.xml", content);
    }
}
