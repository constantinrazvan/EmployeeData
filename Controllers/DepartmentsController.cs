using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Helpers;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

[Authorize]
public class DepartmentsController : Controller
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUsername() => User.Identity?.Name ?? "Unknown";

    private void LogActivity(string action, string details)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Username = GetUsername(),
            Action = action,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            Timestamp = DateTime.Now
        });
        _context.SaveChanges();
    }

    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        int pageSize = 12;
        if (pageNumber < 1) pageNumber = 1;

        var query = _context.Departments.Include(d => d.Employees).AsQueryable();
        var totalCount = await query.CountAsync();

        var departments = await query.OrderBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var pagedResult = new PagedResult<Department>(departments, totalCount, pageNumber, pageSize);

        LogActivity("View Departments", "Viewed the departments dashboard in the organization.");
        return View(pagedResult);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.EmployeesList = await _context.Persons
            .Select(p => p.FirstName + " " + p.LastName)
            .OrderBy(name => name)
            .ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,Description,ManagerName")] Department department)
    {
        if (ModelState.IsValid)
        {
            if (_context.Departments.Any(d => d.Code.ToLower() == department.Code.ToLower()))
            {
                ModelState.AddModelError("Code", "Department code is already in use.");
                ViewBag.EmployeesList = await _context.Persons.Select(p => p.FirstName + " " + p.LastName).OrderBy(n => n).ToListAsync();
                return View(department);
            }

            _context.Add(department);
            await _context.SaveChangesAsync();
            LogActivity("Create Department", $"Created department '{department.Name}' (Code: {department.Code}, Manager: {department.ManagerName}).");
            return RedirectToAction(nameof(Index));
        }

        ViewBag.EmployeesList = await _context.Persons.Select(p => p.FirstName + " " + p.LastName).OrderBy(n => n).ToListAsync();
        return View(department);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();

        var employees = await _context.Persons.Select(p => p.FirstName + " " + p.LastName).OrderBy(n => n).ToListAsync();
        if (!string.IsNullOrEmpty(department.ManagerName) && !employees.Contains(department.ManagerName))
            employees.Insert(0, department.ManagerName);
        ViewBag.EmployeesList = employees;

        return View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,ManagerName")] Department department)
    {
        if (id != department.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                if (_context.Departments.Any(d => d.Code.ToLower() == department.Code.ToLower() && d.Id != department.Id))
                {
                    ModelState.AddModelError("Code", "Department code is already in use.");
                    var emps = await _context.Persons.Select(p => p.FirstName + " " + p.LastName).OrderBy(n => n).ToListAsync();
                    if (!string.IsNullOrEmpty(department.ManagerName) && !emps.Contains(department.ManagerName)) emps.Insert(0, department.ManagerName);
                    ViewBag.EmployeesList = emps;
                    return View(department);
                }

                _context.Update(department);
                await _context.SaveChangesAsync();
                LogActivity("Edit Department", $"Updated details of department '{department.Name}' (Code: {department.Code}, Manager: {department.ManagerName}).");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(department.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        var employees2 = await _context.Persons.Select(p => p.FirstName + " " + p.LastName).OrderBy(n => n).ToListAsync();
        if (!string.IsNullOrEmpty(department.ManagerName) && !employees2.Contains(department.ManagerName)) employees2.Insert(0, department.ManagerName);
        ViewBag.EmployeesList = employees2;
        return View(department);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var department = await _context.Departments.Include(d => d.Employees).FirstOrDefaultAsync(m => m.Id == id);
        if (department == null) return NotFound();

        LogActivity("View Department Details", $"Viewed details of department '{department.Name}' (Code: {department.Code}) and its employee list.");
        return View(department);
    }

    private bool DepartmentExists(int id) => _context.Departments.Any(e => e.Id == id);
}
