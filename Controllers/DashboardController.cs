using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var persons = await _context.Persons.Include(p => p.Department).ToListAsync();
        var departments = await _context.Departments.Include(d => d.Employees).ToListAsync();
        var now = DateTime.Now;

        ViewBag.TotalEmployees = persons.Count;
        ViewBag.ActiveEmployees = persons.Count(p => p.Status == "Active");
        ViewBag.InactiveEmployees = persons.Count(p => p.Status == "Inactive");
        ViewBag.TotalDepartments = departments.Count;

        var last30 = now.AddDays(-30);
        ViewBag.NewHiresLast30 = persons.Count(p => p.HireDate >= last30);
        ViewBag.AnniversariesThisMonth = persons.Count(p => p.HireDate.Month == now.Month && p.HireDate.Year != now.Year);

        ViewBag.DeptLabels = departments.Select(d => d.Name).ToList();
        ViewBag.DeptCounts = departments.Select(d => d.Employees.Count).ToList();

        ViewBag.RecentHires = persons.OrderByDescending(p => p.HireDate).Take(5).ToList();

        if (User.IsInRole("Administrator"))
        {
            ViewBag.RecentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(8)
                .ToListAsync();
        }

        ViewBag.PendingOnboarding = await _context.OnboardingTasks.CountAsync(t => !t.IsCompleted);

        ViewBag.UpcomingAnniversaries = persons
            .Where(p => p.Status == "Active")
            .Where(p => {
                int day = p.HireDate.Day;
                if (p.HireDate.Month == 2 && p.HireDate.Day == 29 && !DateTime.IsLeapYear(now.Year))
                {
                    day = 28;
                }
                var annivThis = new DateTime(now.Year, p.HireDate.Month, day);
                if (annivThis < now.Date) annivThis = annivThis.AddYears(1);
                return (annivThis - now.Date).TotalDays <= 7 && p.HireDate.Year != now.Year;
            })
            .OrderBy(p => {
                int day = p.HireDate.Day;
                if (p.HireDate.Month == 2 && p.HireDate.Day == 29 && !DateTime.IsLeapYear(now.Year))
                {
                    day = 28;
                }
                var annivThis = new DateTime(now.Year, p.HireDate.Month, day);
                if (annivThis < now.Date) annivThis = annivThis.AddYears(1);
                return (annivThis - now.Date).TotalDays;
            })
            .Take(5)
            .ToList();

        ViewBag.UpcomingBirthdays = persons
            .Where(p => p.Status == "Active" && p.BirthDate.HasValue)
            .Where(p => {
                var bday = p.BirthDate!.Value;
                int day = bday.Day;
                if (bday.Month == 2 && bday.Day == 29 && !DateTime.IsLeapYear(now.Year))
                {
                    day = 28;
                }
                var bdayThis = new DateTime(now.Year, bday.Month, day);
                if (bdayThis < now.Date) bdayThis = bdayThis.AddYears(1);
                return (bdayThis - now.Date).TotalDays <= 7;
            })
            .OrderBy(p => {
                var bday = p.BirthDate!.Value;
                int day = bday.Day;
                if (bday.Month == 2 && bday.Day == 29 && !DateTime.IsLeapYear(now.Year))
                {
                    day = 28;
                }
                var bdayThis = new DateTime(now.Year, bday.Month, day);
                if (bdayThis < now.Date) bdayThis = bdayThis.AddYears(1);
                return (bdayThis - now.Date).TotalDays;
            })
            .Take(5)
            .ToList();

        return View();
    }
}
