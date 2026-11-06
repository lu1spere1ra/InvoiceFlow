using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Application.Saft;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/saft")]
[Authorize(Roles = Roles.Writers)]
public sealed class SaftController : ApiControllerBase
{
    /// <summary>SAF-T (PT) 1.04_01 file with every issued document of the period (one fiscal year at most).</summary>
    /// <param name="period">Inclusive dates, e.g. <c>?from=2026-01-01&amp;to=2026-03-31</c>.</param>
    [HttpGet]
    [Produces("application/xml")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "application/xml")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Export([FromQuery] SaftPeriod period, [FromServices] ExportSaft handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(period, cancellationToken);
        return result.IsSuccess ? File(result.Value.Content, "application/xml", result.Value.FileName) : Problem(result.Error);
    }
}
