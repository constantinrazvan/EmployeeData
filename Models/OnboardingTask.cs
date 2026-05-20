using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeData.Models;

public class OnboardingTask
{
    public int Id { get; set; }

    [Required]
    public int PersonId { get; set; }

    public Person? Person { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    [StringLength(100)]
    public string CompletedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int SortOrder { get; set; } = 0;
}
