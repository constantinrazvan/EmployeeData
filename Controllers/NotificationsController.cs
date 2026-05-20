using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;

namespace EmployeeData.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> GetBadge()
    {
        var now = DateTime.Now;
        var notifications = new List<object>();

        var persons = await _context.Persons.Include(p => p.Department).ToListAsync();

        foreach (var p in persons.Where(p => p.Status == "Active"))
        {
            var annivThisYear = new DateTime(now.Year, p.HireDate.Month, p.HireDate.Day);
            if (annivThisYear < now.Date) annivThisYear = annivThisYear.AddYears(1);
            var daysUntil = (annivThisYear - now.Date).TotalDays;
            if (daysUntil <= 7 && p.HireDate.Year != now.Year)
            {
                var years = annivThisYear.Year - p.HireDate.Year;
                notifications.Add(new {
                    type = "anniversary",
                    icon = "calendar",
                    message = $"{p.FullName} - {years} year{(years == 1 ? "" : "s")} anniversary {(daysUntil == 0 ? "today" : $"in {(int)daysUntil} day{((int)daysUntil == 1 ? "" : "s")}")}",
                    link = $"/Employees/Details/{p.Id}",
                    color = "warning"
                });
            }
        }

        var pendingOnboarding = await _context.OnboardingTasks.CountAsync(t => !t.IsCompleted);
        if (pendingOnboarding > 0)
        {
            notifications.Add(new {
                type = "onboarding",
                icon = "clipboard",
                message = $"{pendingOnboarding} onboarding task{(pendingOnboarding == 1 ? "" : "s")} still pending",
                link = "/Employees",
                color = "info"
            });
        }

        if (User.IsInRole("Administrator"))
        {
            var longInactive = persons.Count(p => p.Status == "Inactive" && (now - p.HireDate).TotalDays > 30);
            if (longInactive > 0)
            {
                notifications.Add(new {
                    type = "inactive",
                    icon = "alert",
                    message = $"{longInactive} inactive employee{(longInactive == 1 ? "" : "s")} - review recommended",
                    link = "/Employees?status=Inactive",
                    color = "danger"
                });
            }

            var newHires = persons.Count(p => p.HireDate >= now.AddDays(-7));
            if (newHires > 0)
            {
                notifications.Add(new {
                    type = "newhire",
                    icon = "user-plus",
                    message = $"{newHires} new employee{(newHires == 1 ? "" : "s")} hired in the last 7 days",
                    link = "/Employees",
                    color = "success"
                });
            }
        }

        return Json(new { count = notifications.Count, items = notifications });
    }
}
