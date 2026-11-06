using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/customers")]
public sealed class CustomersController : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<CustomerDto>> List(
        [FromQuery] CustomerListQuery query, [FromServices] ICustomerQueries queries, CancellationToken cancellationToken) =>
        queries.ListAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, [FromServices] ICustomerQueries queries, CancellationToken cancellationToken) =>
        await queries.GetByIdAsync(id, cancellationToken) is { } customer ? customer : NotFound();

    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerDto>> Create(
        CustomerInput input, [FromServices] CreateCustomer handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(input, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error);
    }
}
