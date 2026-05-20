using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated() =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));

    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var persons = await _context.Persons.Include(p => p.Department).ToListAsync();
        var departments = await _context.Departments.Include(d => d.Employees).ToListAsync();
        var now = DateTime.Now;

        ViewBag.TotalEmployees = persons.Count;
        ViewBag.ActiveEmployees = persons.Count(p => p.Status == "Active");
        ViewBag.InactiveEmployees = persons.Count(p => p.Status == "Inactive");
        ViewBag.TotalDepartments = departments.Count;

        var last30 = now.AddDays(-30);
        ViewBag.NewHiresLast30 = persons.Count(p => p.HireDate >= last30);

        ViewBag.AnniversariesThisMonth = persons.Count(p =>
            p.HireDate.Month == now.Month && p.HireDate.Year != now.Year);

        ViewBag.DeptLabels = departments.Select(d => d.Name).ToList();
        ViewBag.DeptCounts = departments.Select(d => d.Employees.Count).ToList();

        ViewBag.RecentHires = persons
            .OrderByDescending(p => p.HireDate)
            .Take(5)
            .ToList();

        var role = HttpContext.Session.GetString("Role");
        if (role == "Administrator")
        {
            ViewBag.RecentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(8)
                .ToListAsync();
        }

        ViewBag.PendingOnboarding = await _context.OnboardingTasks
            .CountAsync(t => !t.IsCompleted);

        ViewBag.UpcomingAnniversaries = persons
            .Where(p => {
                var annivThis = new DateTime(now.Year, p.HireDate.Month, p.HireDate.Day);
                if (annivThis < now.Date) annivThis = annivThis.AddYears(1);
                return (annivThis - now.Date).TotalDays <= 7 && p.HireDate.Year != now.Year;
            })
            .Take(5)
            .ToList();

        return View();
    }
}
