using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EmployeeData.Models.Enums;

namespace EmployeeData.Models;

public class Person
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public DateTime HireDate { get; set; } = DateTime.Today;

    public ContractPeriod ContractPeriod { get; set; } = ContractPeriod.Permanent;
    public DateTime? TerminationDate { get; set; }

    public DateTime? EstimatedTerminationDate
    {
        get
        {
            if (ContractPeriod == ContractPeriod.Permanent)
            {
                return null;
            }
            return ContractPeriod switch
            {
                ContractPeriod.Internship => HireDate.AddMonths(3),
                ContractPeriod.Freelance => HireDate.AddMonths(6),
                ContractPeriod.Temporary => HireDate.AddYears(1),
                ContractPeriod.NotPermanent => HireDate.AddMonths(6),
                _ => HireDate.AddMonths(6)
            };
        }
    }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    public ICollection<AppAccessRole> AppAccessRoles { get; set; } = new List<AppAccessRole>();

    public string FullName => $"{FirstName} {LastName}";
}
