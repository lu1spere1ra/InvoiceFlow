namespace InvoiceFlow.Application.Auth;

/// <summary>The authenticated user of the current request, if any.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }

    string? Email { get; }
}
