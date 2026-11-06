using InvoiceFlow.Application.Common;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public interface IInvoicePdfRenderer
{
    /// <param name="qrPayload">AT QR content, or <c>null</c> for drafts.</param>
    byte[] Render(InvoiceDto invoice, CompanyProfile company, string? qrPayload);
}

public sealed record PdfFile(string FileName, byte[] Content);

public sealed class GetInvoicePdf(IInvoiceQueries queries, IInvoicePdfRenderer renderer, CompanyProfile company)
{
    public async Task<Result<PdfFile>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await queries.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.NotFound(id);
        }

        var content = renderer.Render(invoice, company, AtQrCode.Build(invoice, company));
        var name = (invoice.DocumentNumber ?? $"draft-{invoice.Id:N}").Replace(' ', '_').Replace('/', '-');
        return new PdfFile($"{name}.pdf", content);
    }
}
