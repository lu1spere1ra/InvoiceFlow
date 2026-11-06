using FluentValidation;
using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Application.Auth;

public sealed record LoginInput(string Email, string Password);

public sealed class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed record UserDto(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);

/// <summary>A short-lived access token (JWT) and a long-lived, single-use refresh token.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a refresh token for a new pair (rotation). Presenting an already-used token is treated as
    /// theft: every refresh token of that user is revoked.
    /// </summary>
    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error InvalidRefreshToken = new("Auth.InvalidRefreshToken", "The session has expired. Sign in again.");

    public static readonly Error LockedOut = new("Auth.LockedOut", "Too many failed attempts. Try again in a few minutes.");
}
