using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
