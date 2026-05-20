using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Helpers;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

[Authorize(Roles = "Administrator")]
public class LogsController : Controller
{
    private readonly AppDbContext _context;

    public LogsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm, string actionFilter, int pageNumber = 1)
    {
        int pageSize = 20;
        if (pageNumber < 1) pageNumber = 1;

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var s = searchTerm.ToLower();
            query = query.Where(l => l.Username.ToLower().Contains(s) || l.Details.ToLower().Contains(s) || l.Action.ToLower().Contains(s));
        }

        if (!string.IsNullOrEmpty(actionFilter))
            query = query.Where(l => l.Action == actionFilter);

        var totalCount = await query.CountAsync();
        var logs = await query.OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var pagedResult = new PagedResult<AuditLog>(logs, totalCount, pageNumber, pageSize);

        ViewBag.ActionTypes = await _context.AuditLogs.Select(l => l.Action).Distinct().ToListAsync();
        ViewBag.CurrentSearch = searchTerm;
        ViewBag.CurrentAction = actionFilter;

        return View(pagedResult);
    }
}
