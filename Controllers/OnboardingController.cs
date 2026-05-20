using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

public class OnboardingController : Controller
{
    private readonly AppDbContext _context;

    public OnboardingController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated() =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));

    private string GetUsername() =>
        HttpContext.Session.GetString("Username") ?? "system";

    public async Task<IActionResult> Employee(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        var person = await _context.Persons
            .Include(p => p.Department)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null) return NotFound();

        var tasks = await _context.OnboardingTasks
            .Where(t => t.PersonId == id)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        ViewBag.Person = person;
        ViewBag.CompletedCount = tasks.Count(t => t.IsCompleted);
        ViewBag.TotalCount = tasks.Count;

        return View(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int taskId, int personId)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        var task = await _context.OnboardingTasks.FindAsync(taskId);
        if (task == null) return NotFound();

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.Now : null;
        task.CompletedBy = task.IsCompleted ? GetUsername() : string.Empty;

        await _context.SaveChangesAsync();

        return RedirectToAction("Employee", new { id = personId });
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(int personId, string title, string description)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        if (!string.IsNullOrWhiteSpace(title))
        {
            var maxOrder = await _context.OnboardingTasks
                .Where(t => t.PersonId == personId)
                .MaxAsync(t => (int?)t.SortOrder) ?? 0;

            _context.OnboardingTasks.Add(new OnboardingTask
            {
                PersonId = personId,
                Title = title.Trim(),
                Description = description?.Trim() ?? string.Empty,
                SortOrder = maxOrder + 1,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Employee", new { id = personId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTask(int taskId, int personId)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        var task = await _context.OnboardingTasks.FindAsync(taskId);
        if (task != null)
        {
            _context.OnboardingTasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Employee", new { id = personId });
    }

    public static List<OnboardingTask> CreateDefaultTasks(int personId)
    {
        var tasks = new[]
        {
            ("Create company email account", "Set up the official email address in the company domain."),
            ("Grant application access", "Assign the required application access roles based on the employee's role."),
            ("Sign employment contract", "Collect the signed employment agreement and file it in HR records."),
            ("HR orientation session", "Schedule and complete the onboarding orientation with HR."),
            ("Allocate equipment", "Assign laptop, badge, and any other required equipment."),
            ("Team introduction", "Introduce the new employee to their team and manager."),
            ("Setup workstation", "Ensure workstation is configured and all tools are installed."),
            ("Review company policies", "Employee reads and acknowledges company policies and code of conduct.")
        };

        return tasks.Select((t, i) => new OnboardingTask
        {
            PersonId = personId,
            Title = t.Item1,
            Description = t.Item2,
            SortOrder = i + 1,
            CreatedAt = DateTime.Now
        }).ToList();
    }
}
