using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

public class SettingsController : Controller
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
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

    public IActionResult Index()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdmin())
        {
            TempData["Error"] = "Access denied. Only administrators can access this page.";
            return RedirectToAction("Index", "Employees");
        }

        var settings = SettingsService.GetSettings();

        LogActivity("View Settings", "Viewed the administrative settings panel.");

        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSettings(SystemSettings settings)
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdmin())
        {
            TempData["Error"] = "Access denied. Only administrators can perform this action.";
            return RedirectToAction("Index", "Employees");
        }

        SettingsService.SaveSettings(settings);

        LogActivity("Save Settings", "Updated system settings (SMTP, Database, Email Templates).");
        TempData["Success"] = "Settings have been saved successfully!";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMigrations()
    {
        if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
        if (!IsAdmin())
        {
            TempData["Error"] = "Access denied. Only administrators can perform this action.";
            return RedirectToAction("Index", "Employees");
        }

        try
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            DbSeeder.Seed(_context);

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
        if (!IsAuthenticated()) return Json(new { success = false, message = "Unauthenticated" });
        if (!IsAdmin()) return Json(new { success = false, message = "Access denied. Only an Administrator can test connection." });

        if (string.IsNullOrEmpty(host))
        {
            return Json(new { success = false, message = "SMTP server host is required." });
        }

        try
        {
            using (var tcpClient = new System.Net.Sockets.TcpClient())
            {
                var ar = tcpClient.BeginConnect(host, port, null, null);
                var wh = ar.AsyncWaitHandle;
                try
                {
                    if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false))
                    {
                        tcpClient.Close();
                        return Json(new { success = false, message = $"Connection to {host}:{port} failed (Timeout - server is not responding)." });
                    }

                    tcpClient.EndConnect(ar);
                }
                finally
                {
                    wh.Close();
                }
            }

            return Json(new { success = true, message = $"Connection to SMTP server ({host}:{port}) was established successfully! Host is active and responding." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Connection error: {ex.Message}" });
        }
    }
}
