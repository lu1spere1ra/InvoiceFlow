using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

/// <summary>
/// The access token is returned in the body and kept in memory by the SPA. The refresh token travels
/// only in an HttpOnly cookie scoped to this controller, so page scripts can never read it (ADR 0007).
/// </summary>
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    public const string RefreshCookie = "invoiceflow_refresh";

    public sealed record SessionDto(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);

    [HttpPost("login")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionDto>> Login(LoginInput input, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(input, cancellationToken);
        return result.IsSuccess ? StartSession(result.Value) : Unauthorized(result.Error);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SessionDto>> Refresh(CancellationToken cancellationToken)
    {
        if (Request.Cookies[RefreshCookie] is not { Length: > 0 } token)
        {
            return Unauthorized(AuthErrors.InvalidRefreshToken);
        }

        var result = await auth.RefreshAsync(token, cancellationToken);
        if (result.IsFailure)
        {
            Response.Cookies.Delete(RefreshCookie, CookieOptions(DateTimeOffset.UnixEpoch));
            return Unauthorized(result.Error);
        }

        return StartSession(result.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies[RefreshCookie] is { Length: > 0 } token)
        {
            await auth.LogoutAsync(token, cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookie, CookieOptions(DateTimeOffset.UnixEpoch));
        return NoContent();
    }

    private SessionDto StartSession(AuthTokens tokens)
    {
        Response.Cookies.Append(RefreshCookie, tokens.RefreshToken, CookieOptions(tokens.RefreshTokenExpiresAt));
        return new SessionDto(tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.User);
    }

    private CookieOptions CookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expires,
    };

    private ObjectResult Unauthorized(Error error)
    {
        var problem = Problem(error);
        problem.StatusCode = StatusCodes.Status401Unauthorized;
        if (problem.Value is ProblemDetails details)
        {
            details.Status = StatusCodes.Status401Unauthorized;
            details.Title = "Unauthorized";
        }

        return problem;
    }
}
