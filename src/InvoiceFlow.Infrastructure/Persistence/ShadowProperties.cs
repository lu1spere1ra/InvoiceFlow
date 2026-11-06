namespace InvoiceFlow.Infrastructure.Persistence;

/// <summary>Columns maintained by interceptors and invisible to the domain model.</summary>
internal static class ShadowProperties
{
    public const string CreatedAt = nameof(CreatedAt);
    public const string CreatedBy = nameof(CreatedBy);
    public const string UpdatedAt = nameof(UpdatedAt);
    public const string UpdatedBy = nameof(UpdatedBy);
    public const string IsDeleted = nameof(IsDeleted);
    public const string DeletedAt = nameof(DeletedAt);
}
