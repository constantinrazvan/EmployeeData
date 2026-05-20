using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string Details { get; set; } = string.Empty;

    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
