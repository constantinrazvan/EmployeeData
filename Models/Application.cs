using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class Application
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(200)]
    public string Url { get; set; } = string.Empty;

    public ICollection<AppAccessRole> AppAccessRoles { get; set; } = new List<AppAccessRole>();
}
