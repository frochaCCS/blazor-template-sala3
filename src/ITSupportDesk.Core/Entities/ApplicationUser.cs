using Microsoft.AspNetCore.Identity;

namespace ITSupportDesk.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
