using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class AppAccessRole
{
    public int Id { get; set; }

    [Required]
    public int PersonId { get; set; }

    public Person? Person { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    public Application? Application { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    public DateTime GrantedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(100)]
    public string GrantedBy { get; set; } = string.Empty;
}
