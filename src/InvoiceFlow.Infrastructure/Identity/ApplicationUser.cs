using Microsoft.AspNetCore.Identity;

namespace InvoiceFlow.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = "";
}
