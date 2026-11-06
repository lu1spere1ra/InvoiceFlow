using System.ComponentModel.DataAnnotations;

namespace InvoiceFlow.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "InvoiceFlow";

    [Required]
    public string Audience { get; init; } = "InvoiceFlow";

    /// <summary>HMAC-SHA256 key; at least 32 bytes. Comes from configuration, never from source in production.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = "";

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;
}
