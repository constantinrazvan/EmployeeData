using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated() =>
        !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));

    private bool IsAdminOrHr()
    {
        var role = HttpContext.Session.GetString("Role");
        return role == "Administrator" || role == "HR";
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdminOrHr()) return RedirectToAction("Index", "Employees");

        var persons = await _context.Persons.Include(p => p.Department).ToListAsync();
        var departments = await _context.Departments.Include(d => d.Employees).ToListAsync();
        var now = DateTime.Now;

        ViewBag.HeadcountData = departments.Select(d => new {
            Department = d.Name,
            Code = d.Code,
            Total = d.Employees.Count,
            Active = d.Employees.Count(e => e.Status == "Active"),
            Inactive = d.Employees.Count(e => e.Status == "Inactive"),
            Manager = d.ManagerName
        }).OrderByDescending(x => x.Total).ToList();

        ViewBag.TenureBands = new[] {
            new { Label = "< 1 year",   Count = persons.Count(p => (now - p.HireDate).TotalDays < 365) },
            new { Label = "1-2 years",  Count = persons.Count(p => (now - p.HireDate).TotalDays >= 365 && (now - p.HireDate).TotalDays < 730) },
            new { Label = "2-5 years",  Count = persons.Count(p => (now - p.HireDate).TotalDays >= 730 && (now - p.HireDate).TotalDays < 1825) },
            new { Label = "5-10 years", Count = persons.Count(p => (now - p.HireDate).TotalDays >= 1825 && (now - p.HireDate).TotalDays < 3650) },
            new { Label = "> 10 years", Count = persons.Count(p => (now - p.HireDate).TotalDays >= 3650) }
        };

        ViewBag.TotalActive = persons.Count(p => p.Status == "Active");
        ViewBag.TotalInactive = persons.Count(p => p.Status == "Inactive");
        ViewBag.TurnoverRate = persons.Any()
            ? Math.Round((double)persons.Count(p => p.Status == "Inactive") / persons.Count * 100, 1)
            : 0;

        ViewBag.HiresPerMonth = Enumerable.Range(0, 12)
            .Select(i => now.AddMonths(-11 + i))
            .Select(m => new {
                Month = m.ToString("MMM yy"),
                Count = persons.Count(p => p.HireDate.Month == m.Month && p.HireDate.Year == m.Year)
            }).ToList();

        ViewBag.MostTenured = persons
            .OrderBy(p => p.HireDate)
            .Take(5)
            .Select(p => new {
                p.FullName,
                p.Position,
                Department = p.Department?.Name ?? "-",
                Years = Math.Floor((now - p.HireDate).TotalDays / 365)
            }).ToList();

        return View();
    }

    public async Task<IActionResult> ExportHeadcount()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdminOrHr()) return RedirectToAction("Index", "Employees");

        var departments = await _context.Departments.Include(d => d.Employees).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Department,Code,Manager,Total Employees,Active,Inactive");
        foreach (var d in departments.OrderByDescending(d => d.Employees.Count))
        {
            sb.AppendLine($"\"{d.Name}\",\"{d.Code}\",\"{d.ManagerName}\",{d.Employees.Count},{d.Employees.Count(e => e.Status == "Active")},{d.Employees.Count(e => e.Status == "Inactive")}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"headcount_report_{DateTime.Now:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> ExportEmployees()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdminOrHr()) return RedirectToAction("Index", "Employees");

        var persons = await _context.Persons.Include(p => p.Department).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Full Name,First Name,Last Name,Email,Phone,Position,Department,Hire Date,Status,Tenure (years)");
        foreach (var p in persons.OrderBy(p => p.LastName))
        {
            var tenure = Math.Floor((DateTime.Now - p.HireDate).TotalDays / 365);
            sb.AppendLine($"\"{p.FullName}\",\"{p.FirstName}\",\"{p.LastName}\",\"{p.Email}\",\"{p.Phone}\",\"{p.Position}\",\"{p.Department?.Name}\",{p.HireDate:yyyy-MM-dd},{p.Status},{tenure}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"employees_export_{DateTime.Now:yyyyMMdd}.csv");
    }
}
