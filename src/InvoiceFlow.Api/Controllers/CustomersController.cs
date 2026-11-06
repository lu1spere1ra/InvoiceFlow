using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Application.Common;
using InvoiceFlow.Application.Customers;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Roles = Roles.Writers)]
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

    [Authorize(Roles = Roles.Writers)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerDto>> Update(
        Guid id, CustomerInput input, [FromServices] UpdateCustomer handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    /// <summary>Hides the customer from lists and pickers. Existing documents are not affected.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteCustomer handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
