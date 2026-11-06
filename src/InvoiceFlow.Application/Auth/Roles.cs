namespace InvoiceFlow.Application.Auth;

public static class Roles
{
    /// <summary>Everything, including users and series management.</summary>
    public const string Admin = "Admin";

    /// <summary>Creates, issues and voids documents; manages customers and products.</summary>
    public const string Accountant = "Accountant";

    /// <summary>Read-only access.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Roles allowed to change data, for <c>[Authorize(Roles = Roles.Writers)]</c>.</summary>
    public const string Writers = Admin + "," + Accountant;

    public static readonly IReadOnlyList<string> All = [Admin, Accountant, Viewer];
}
