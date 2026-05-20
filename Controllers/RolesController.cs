using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

[Authorize(Roles = "Administrator")]
public class RolesController : Controller
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
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

    public async Task<IActionResult> Index(int? personId, string activeTab = "matrix", int pageNumber = 1)
    {
        int pageSize = 15;
        if (pageNumber < 1) pageNumber = 1;

        var query = _context.Persons
            .Include(p => p.Department)
            .Include(p => p.AppAccessRoles).ThenInclude(ar => ar.Application)
            .OrderBy(p => p.LastName)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var employees = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        var pagedEmployees = new PagedResult<Person>(employees, totalCount, pageNumber, pageSize);

        var applications = await _context.Applications.OrderBy(a => a.Name).ToListAsync();

        ViewBag.Employees = employees;
        ViewBag.Applications = applications;
        ViewBag.SelectedPersonId = personId;
        ViewBag.ActiveTab = activeTab;
        ViewBag.PagedEmployees = pagedEmployees;

        Person? selectedPerson = null;
        if (personId.HasValue)
        {
            selectedPerson = await _context.Persons
                .Include(p => p.Department)
                .Include(p => p.AppAccessRoles).ThenInclude(ar => ar.Application)
                .FirstOrDefaultAsync(p => p.Id == personId.Value);

            if (selectedPerson != null)
            {
                var activeAppIds = selectedPerson.AppAccessRoles.Select(ar => ar.ApplicationId).ToList();
                ViewBag.AvailableApplications = new SelectList(applications.Where(a => !activeAppIds.Contains(a.Id)).ToList(), "Id", "Name");
            }
        }

        LogActivity("View Roles", $"Accessed the application access control panel (Active tab: {activeTab}, Page: {pageNumber}).");
        return View(selectedPerson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grant(int personId, int applicationId, string roleName, string customRoleName)
    {
        string finalRoleName = (roleName == "Other..." || string.IsNullOrEmpty(roleName)) ? customRoleName : roleName;

        if (string.IsNullOrEmpty(finalRoleName))
        {
            TempData["Error"] = "Role name is required.";
            return RedirectToAction(nameof(Index), new { personId, activeTab = "individual" });
        }

        if (await _context.AppAccessRoles.AnyAsync(ar => ar.PersonId == personId && ar.ApplicationId == applicationId))
        {
            TempData["Error"] = "The employee already has access to this application.";
            return RedirectToAction(nameof(Index), new { personId, activeTab = "individual" });
        }

        _context.AppAccessRoles.Add(new AppAccessRole
        {
            PersonId = personId,
            ApplicationId = applicationId,
            RoleName = finalRoleName,
            GrantedAt = DateTime.Now,
            GrantedBy = GetUsername()
        });
        await _context.SaveChangesAsync();

        var person = await _context.Persons.FindAsync(personId);
        var app = await _context.Applications.FindAsync(applicationId);

        LogActivity("Grant Access", $"Granted role '{finalRoleName}' on application '{app?.Name}' to employee '{person?.FullName}' (ID: {personId}).");

        if (person != null && app != null)
        {
            var emailParams = new Dictionary<string, string>
            {
                { "EmployeeName", person.FullName },
                { "AppName", app.Name },
                { "RoleName", finalRoleName },
                { "AppUrl", app.Url },
                { "GrantedBy", GetUsername() }
            };
            var emailResult = SettingsService.SendNotification("access_granted", emailParams, person.Email);
            LogActivity("Send Notification", $"Email sending result: {emailResult.LogMessage}");
            TempData["Success"] = $"Access has been granted. {emailResult.LogMessage}";
        }
        else
        {
            TempData["Success"] = $"Access to application '{app?.Name}' with role '{finalRoleName}' was granted.";
        }

        return RedirectToAction(nameof(Index), new { personId, activeTab = "individual" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int id, string sourceTab = "individual")
    {
        var appRole = await _context.AppAccessRoles
            .Include(ar => ar.Person).Include(ar => ar.Application)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (appRole == null) return NotFound();

        int personId = appRole.PersonId;
        string personName = appRole.Person?.FullName ?? "Unknown";
        string personEmail = appRole.Person?.Email ?? "";
        string appName = appRole.Application?.Name ?? "Unknown";
        string appUrl = appRole.Application?.Url ?? "";
        string roleName = appRole.RoleName;

        _context.AppAccessRoles.Remove(appRole);
        await _context.SaveChangesAsync();

        LogActivity("Revoke Access", $"Revoked role '{roleName}' on application '{appName}' for employee '{personName}' (ID: {personId}).");

        if (!string.IsNullOrEmpty(personEmail))
        {
            var emailParams = new Dictionary<string, string>
            {
                { "EmployeeName", personName },
                { "AppName", appName },
                { "RoleName", roleName },
                { "AppUrl", appUrl },
                { "GrantedBy", GetUsername() }
            };
            var emailResult = SettingsService.SendNotification("access_revoked", emailParams, personEmail);
            LogActivity("Send Notification", $"Email sending result: {emailResult.LogMessage}");
            TempData["Success"] = $"Access has been revoked. {emailResult.LogMessage}";
        }
        else
        {
            TempData["Success"] = $"Access to application '{appName}' was revoked.";
        }

        return RedirectToAction(nameof(Index), new { personId, activeTab = sourceTab });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApplication(string name, string description, string url)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            TempData["Error"] = "Application name and URL are required.";
            return RedirectToAction(nameof(Index), new { activeTab = "applications" });
        }

        _context.Applications.Add(new Application { Name = name, Description = description ?? string.Empty, Url = url });
        await _context.SaveChangesAsync();

        LogActivity("Create Application", $"Registered a new application in the portal: '{name}' ({url}).");
        TempData["Success"] = $"Application '{name}' was registered successfully.";
        return RedirectToAction(nameof(Index), new { activeTab = "applications" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditApplication(int id, string name, string description, string url)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return NotFound();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            TempData["Error"] = "Application name and URL are required.";
            return RedirectToAction(nameof(Index), new { activeTab = "applications" });
        }

        app.Name = name;
        app.Description = description ?? string.Empty;
        app.Url = url;
        _context.Update(app);
        await _context.SaveChangesAsync();

        LogActivity("Edit Application", $"Updated details of application '{name}' ({url}).");
        TempData["Success"] = $"Application '{name}' was updated successfully.";
        return RedirectToAction(nameof(Index), new { activeTab = "applications" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return NotFound();

        string appName = app.Name;
        _context.Applications.Remove(app);
        await _context.SaveChangesAsync();

        LogActivity("Delete Application", $"Deleted application '{appName}' and all associated cascade access roles.");
        TempData["Success"] = $"Application '{appName}' and all associated permissions have been deleted.";
        return RedirectToAction(nameof(Index), new { activeTab = "applications" });
    }
}
