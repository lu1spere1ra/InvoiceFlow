using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Application.Series;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/series")]
public sealed class SeriesController : ApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SeriesDto>> List([FromServices] ISeriesQueries queries, CancellationToken cancellationToken) =>
        queries.ListAsync(cancellationToken);

    /// <summary>Opens a new series and obtains its validation code (simulated AT communication).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<SeriesDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeriesDto>> Create(
        CreateSeriesInput input, [FromServices] CreateSeries handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(input, cancellationToken);
        return result.IsSuccess ? Created($"/api/series/{result.Value.Id}", result.Value) : Problem(result.Error);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType<SeriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesDto>> Close(Guid id, [FromServices] CloseSeries handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    /// <summary>Recomputes every signature of the series and reports the first broken document, if any.</summary>
    [HttpGet("{id:guid}/verification")]
    [ProducesResponseType<SeriesVerificationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesVerificationDto>> Verify(
        Guid id, [FromServices] VerifySeries handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }
}
