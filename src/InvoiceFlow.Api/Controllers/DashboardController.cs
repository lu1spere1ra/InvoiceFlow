using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get([FromServices] IDashboardQueries queries, CancellationToken cancellationToken) =>
        queries.GetAsync(cancellationToken);
}
