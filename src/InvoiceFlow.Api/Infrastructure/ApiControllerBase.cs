using InvoiceFlow.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Infrastructure;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Maps an expected business failure to an RFC 9457 ProblemDetails response (ADR 0005).</summary>
    protected ObjectResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, detail: error.Message);
        problem.Extensions["code"] = error.Code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
