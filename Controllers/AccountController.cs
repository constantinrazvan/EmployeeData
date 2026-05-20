using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using EmployeeData.Data;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index", "Employees");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Username and password are required.");
            return View();
        }

        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user != null && PasswordHasher.VerifyPassword(password, user.PasswordHash))
        {
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            var log = new AuditLog
            {
                Username = user.Username,
                Action = "Login",
                Details = $"User '{user.FullName}' ({user.Role}) successfully logged in.",
                IpAddress = ipAddress,
                Timestamp = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            _context.SaveChanges();

            return RedirectToAction("Index", "Employees");
        }

        ModelState.AddModelError("", "Incorrect username or password.");

        var failedLog = new AuditLog
        {
            Username = string.IsNullOrEmpty(username) ? "Unauthenticated" : username,
            Action = "Login Failure",
            Details = $"Failed login attempt for username '{username}'.",
            IpAddress = ipAddress,
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(failedLog);
        _context.SaveChanges();

        return View();
    }

    [HttpGet]
    public IActionResult Logout()
    {
        var username = HttpContext.Session.GetString("Username") ?? "Unauthenticated";
        var fullName = HttpContext.Session.GetString("FullName") ?? "";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (username != "Unauthenticated")
        {
            var log = new AuditLog
            {
                Username = username,
                Action = "Logout",
                Details = $"User '{fullName}' logged out from the application.",
                IpAddress = ipAddress,
                Timestamp = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            _context.SaveChanges();
        }

        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
