using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

[Authorize(Roles = "Administrator")]
public class SettingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public SettingsController(AppDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
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

    public IActionResult Index()
    {
        var settings = SettingsService.GetSettings();
        LogActivity("View Settings", "Viewed the administrative settings panel.");
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSettings(SystemSettings settings)
    {
        SettingsService.SaveSettings(settings);
        LogActivity("Save Settings", "Updated system settings (SMTP, Database, Email Templates).");
        TempData["Success"] = "Settings have been saved successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMigrations()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            await DbSeeder.Seed(_context, _userManager, _roleManager);

            LogActivity("Run Migrations", "Recreated database and repopulated it with system test data.");
            TempData["Success"] = "Database has been successfully wiped, recreated, and seeded with demo data!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Migration error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TestSmtpConnection(string host, int port, string username, string password, bool enableSsl, string fromEmail)
    {
        if (string.IsNullOrEmpty(host))
            return Json(new { success = false, message = "SMTP server host is required." });

        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var ar = tcpClient.BeginConnect(host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false))
            {
                tcpClient.Close();
                return Json(new { success = false, message = $"Connection to {host}:{port} failed (Timeout - server is not responding)." });
            }
            tcpClient.EndConnect(ar);
            return Json(new { success = true, message = $"Connection to SMTP server ({host}:{port}) was established successfully! Host is active and responding." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Connection error: {ex.Message}" });
        }
    }
}
