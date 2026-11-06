using System.Security.Claims;
using InvoiceFlow.Application.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InvoiceFlow.Api.Infrastructure;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? Id => Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);
}
