using Microsoft.AspNetCore.Identity;

namespace PropertyManagement.Models;

public class AspNetUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}