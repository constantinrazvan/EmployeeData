using Microsoft.AspNetCore.Identity;

namespace EmployeeData.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
