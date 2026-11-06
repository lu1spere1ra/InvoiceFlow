using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/invoices")]
public sealed class InvoicesController : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<InvoiceSummaryDto>> List(
        [FromQuery] InvoiceListQuery query, [FromServices] IInvoiceQueries queries, CancellationToken cancellationToken) =>
        queries.ListAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id, [FromServices] IInvoiceQueries queries, CancellationToken cancellationToken) =>
        await queries.GetByIdAsync(id, cancellationToken) is { } invoice ? invoice : NotFound();

    /// <summary>Creates a draft. Drafts have no number and can be edited or deleted.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPost]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Create(
        CreateInvoiceDraftInput input, [FromServices] CreateInvoiceDraft handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }

    /// <summary>Replaces customer, payment terms and lines of a draft.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Update(
        Guid id, UpdateInvoiceDraftInput input, [FromServices] UpdateInvoiceDraft handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    [Authorize(Roles = Roles.Writers)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteInvoiceDraft handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Issues a draft: assigns the next number of the active series and makes it immutable.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPost("{id:guid}/issue")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Issue(Guid id, [FromServices] IssueInvoice handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    /// <summary>Records the payment of an issued invoice (FT). Defaults to today.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> Pay(
        Guid id, MarkPaidInput input, [FromServices] MarkInvoicePaid handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    /// <summary>Voids an issued document. It keeps its number and stays in the SAF-T file as cancelled.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Void(
        Guid id, VoidInput input, [FromServices] VoidInvoice handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    /// <summary>Creates a draft credit note against an issued invoice.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPost("{id:guid}/credit-notes")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceDto>> CreateCreditNote(
        Guid id, CreateCreditNoteInput input, [FromServices] CreateCreditNote handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }

    /// <summary>The document as an A4 PDF with the AT QR code. Drafts are marked as not valid.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Pdf(Guid id, [FromServices] GetInvoicePdf handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? File(result.Value.Content, "application/pdf", result.Value.FileName) : Problem(result.Error);
    }
}
