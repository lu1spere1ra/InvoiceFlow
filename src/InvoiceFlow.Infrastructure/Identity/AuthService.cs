using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InvoiceFlow.Application.Auth;
using InvoiceFlow.Domain.Common;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceFlow.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<ApplicationUser> users,
    AppDbContext db,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<Result<AuthTokens>> LoginAsync(LoginInput input, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(input.Email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (await users.IsLockedOutAsync(user))
        {
            return AuthErrors.LockedOut;
        }

        if (!await users.CheckPasswordAsync(user, input.Password))
        {
            await users.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await users.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == Hash(refreshToken), cancellationToken);
        if (stored is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        if (stored.RevokedAt is not null)
        {
            // A rotated token came back: either a replay or a stolen token. Revoke the whole session set.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevokedAt, now), cancellationToken);
            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var user = await users.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        stored.RevokedAt = now;
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.TokenHash == Hash(refreshToken) && t.RevokedAt == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevokedAt, timeProvider.GetUtcNow()), cancellationToken);

    private async Task<AuthTokens> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var roles = (await users.GetRolesAsync(user)).ToList();

        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = accessExpires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256),
        });

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpires,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            accessToken, accessExpires, refreshToken, refreshExpires, new UserDto(user.Id, user.Email!, user.FullName, roles));
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
