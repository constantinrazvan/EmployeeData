using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class Department
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ManagerName { get; set; } = string.Empty;

    public ICollection<Person> Employees { get; set; } = new List<Person>();
}
