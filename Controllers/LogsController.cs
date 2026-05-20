using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

public class LogsController : Controller
{
    private readonly AppDbContext _context;

    public LogsController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
    }

    private bool IsAdmin()
    {
        return HttpContext.Session.GetString("Role") == "Administrator";
    }

    public async Task<IActionResult> Index(string searchTerm, string actionFilter, int pageNumber = 1)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdmin())
        {
            TempData["Error"] = "Access denied. Only administrators can access the activity log.";
            return RedirectToAction("Index", "Employees");
        }

        int pageSize = 20;
        if (pageNumber < 1) pageNumber = 1;

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(l => l.Username.ToLower().Contains(searchLower) ||
                                     l.Details.ToLower().Contains(searchLower) ||
                                     l.Action.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrEmpty(actionFilter))
        {
            query = query.Where(l => l.Action == actionFilter);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pagedResult = new PagedResult<AuditLog>(logs, totalCount, pageNumber, pageSize);

        ViewBag.ActionTypes = await _context.AuditLogs
            .Select(l => l.Action)
            .Distinct()
            .ToListAsync();

        ViewBag.CurrentSearch = searchTerm;
        ViewBag.CurrentAction = actionFilter;

        return View(pagedResult);
    }
}
