using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

public class EmployeesController : Controller
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
    }

    private string GetUsername()
    {
        return HttpContext.Session.GetString("Username") ?? "Unauthenticated";
    }

    private void LogActivity(string action, string details)
    {
        var log = new AuditLog
        {
            Username = GetUsername(),
            Action = action,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(log);
        _context.SaveChanges();
    }

    public async Task<IActionResult> Index(string searchTerm, int? departmentId, string status, int pageNumber = 1)
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction("Login", "Account");
        }

        int pageSize = 10;
        if (pageNumber < 1) pageNumber = 1;

        var query = _context.Persons.Include(p => p.Department).AsQueryable();

        bool hasSearch = !string.IsNullOrEmpty(searchTerm) || departmentId.HasValue || !string.IsNullOrEmpty(status);
        if (hasSearch)
        {
            var filters = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(searchTerm)) filters.Add($"term='{searchTerm}'");
            if (departmentId.HasValue)
            {
                var deptName = _context.Departments.Find(departmentId.Value)?.Code ?? departmentId.Value.ToString();
                filters.Add($"department='{deptName}'");
            }
            if (!string.IsNullOrEmpty(status)) filters.Add($"status='{status}'");

            LogActivity("Search", $"Searched for employees in organization by filters: {string.Join(", ", filters)}.");
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p => p.FirstName.ToLower().Contains(searchLower) ||
                                     p.LastName.ToLower().Contains(searchLower) ||
                                     p.Email.ToLower().Contains(searchLower) ||
                                     p.Phone.Contains(searchTerm) ||
                                     p.Position.ToLower().Contains(searchLower));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(p => p.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pagedResult = new PagedResult<Person>(employees, totalCount, pageNumber, pageSize);

        var allPersons = await _context.Persons.ToListAsync();
        ViewBag.TotalCount = allPersons.Count;
        ViewBag.ActiveCount = allPersons.Count(p => p.Status == "Active");
        ViewBag.InactiveCount = allPersons.Count(p => p.Status == "Inactive");
        ViewBag.DepartmentsCount = await _context.Departments.CountAsync();

        ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", departmentId);
        ViewBag.CurrentSearch = searchTerm;
        ViewBag.CurrentDept = departmentId;
        ViewBag.CurrentStatus = status;

        return View(pagedResult);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (id == null) return NotFound();

        var person = await _context.Persons
            .Include(p => p.Department)
            .Include(p => p.AppAccessRoles)
                .ThenInclude(ar => ar.Application)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (person == null) return NotFound();

        LogActivity("View Details", $"Viewed the detailed profile of employee '{person.FullName}' (ID: {person.Id}).");

        return View(person);
    }

    public async Task<IActionResult> Create()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Position,DepartmentId,HireDate,Status")] Person person)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        if (ModelState.IsValid)
        {
            _context.Add(person);
            await _context.SaveChangesAsync();

            var onboardingTasks = OnboardingController.CreateDefaultTasks(person.Id);
            _context.OnboardingTasks.AddRange(onboardingTasks);
            await _context.SaveChangesAsync();

            var dept = await _context.Departments.FindAsync(person.DepartmentId);
            LogActivity("Add Employee", $"Registered a new employee: '{person.FullName}' as '{person.Position}' (Department: {dept?.Name ?? "Unknown"}). Onboarding checklist generated automatically.");

            return RedirectToAction(nameof(Index));
        }

        ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", person.DepartmentId);
        return View(person);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (id == null) return NotFound();

        var person = await _context.Persons.FindAsync(id);
        if (person == null) return NotFound();

        ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", person.DepartmentId);
        return View(person);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Phone,Position,DepartmentId,HireDate,Status")] Person person)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (id != person.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var oldPerson = await _context.Persons.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                _context.Update(person);
                await _context.SaveChangesAsync();

                var dept = await _context.Departments.FindAsync(person.DepartmentId);
                var changes = $"Name: '{person.FullName}', Position: '{person.Position}', Department: {dept?.Name}, Status: {person.Status}";
                if (oldPerson != null && oldPerson.Status != person.Status)
                {
                    changes += $" (Status changed from {oldPerson.Status} to {person.Status})";
                }
                LogActivity("Edit Employee", $"Updated details of employee '{person.FullName}' (ID: {id}). Current details: {changes}.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(person.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DepartmentId = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", person.DepartmentId);
        return View(person);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (id == null) return NotFound();

        var person = await _context.Persons
            .Include(p => p.Department)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (person == null) return NotFound();

        return View(person);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        var person = await _context.Persons.FindAsync(id);
        if (person != null)
        {
            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            LogActivity("Delete Employee", $"Permanently deleted employee '{person.FullName}' (ID: {id}, Position: {person.Position}). All associated permissions were revoked.");
        }

        return RedirectToAction(nameof(Index));
    }

    private bool PersonExists(int id)
    {
        return _context.Persons.Any(e => e.Id == id);
    }

    public async Task<IActionResult> ExportCsv(string searchTerm, int? departmentId, string status)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

        var query = _context.Persons.Include(p => p.Department).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var s = searchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(s) ||
                p.LastName.ToLower().Contains(s) ||
                p.Email.ToLower().Contains(s) ||
                p.Position.ToLower().Contains(s));
        }
        if (departmentId.HasValue)
            query = query.Where(p => p.DepartmentId == departmentId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var persons = await query.OrderBy(p => p.LastName).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Full Name,First Name,Last Name,Email,Phone,Position,Department,Hire Date,Status,Tenure (years)");
        foreach (var p in persons)
        {
            var tenure = Math.Floor((DateTime.Now - p.HireDate).TotalDays / 365);
            sb.AppendLine($"\"{p.FullName}\",\"{p.FirstName}\",\"{p.LastName}\",\"{p.Email}\",\"{p.Phone}\",\"{p.Position}\",\"{p.Department?.Name}\",{p.HireDate:yyyy-MM-dd},{p.Status},{tenure}");
        }

        LogActivity("Export CSV", $"Exported {persons.Count} employee records to CSV.");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"employees_{DateTime.Now:yyyyMMdd}.csv");
    }
}
