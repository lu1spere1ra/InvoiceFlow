using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/products")]
public sealed class ProductsController : ApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProductDto>> List([FromServices] ICatalogQueries queries, CancellationToken cancellationToken) =>
        queries.ListProductsAsync(cancellationToken);

    [Authorize(Roles = Roles.Writers)]
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        ProductInput input, [FromServices] CreateProduct handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(input, cancellationToken);
        return result.IsSuccess ? Created($"/api/products/{result.Value.Id}", result.Value) : Problem(result.Error);
    }

    /// <summary>Updates a product. The code is immutable; a different code in the body is ignored.</summary>
    [Authorize(Roles = Roles.Writers)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id, ProductInput input, [FromServices] UpdateProduct handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, input, cancellationToken);
        return result.IsSuccess ? result.Value : Problem(result.Error);
    }

    [Authorize(Roles = Roles.Writers)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromServices] DeleteProduct handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
