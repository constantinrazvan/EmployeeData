using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmployeeData.Data;
using EmployeeData.Models;

namespace EmployeeData.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AppDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Employees");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Username and password are required.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(username);
            var log = new AuditLog
            {
                Username = username,
                Action = "Login",
                Details = $"User '{user?.FullName}' successfully logged in.",
                IpAddress = ipAddress,
                Timestamp = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Employees");
        }

        ModelState.AddModelError("", "Incorrect username or password.");

        var failedLog = new AuditLog
        {
            Username = username,
            Action = "Login Failure",
            Details = $"Failed login attempt for username '{username}'.",
            IpAddress = ipAddress,
            Timestamp = DateTime.Now
        };
        _context.AuditLogs.Add(failedLog);
        await _context.SaveChangesAsync();

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name ?? "Unauthenticated";
        var user = await _userManager.FindByNameAsync(username);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (username != "Unauthenticated")
        {
            var log = new AuditLog
            {
                Username = username,
                Action = "Logout",
                Details = $"User '{user?.FullName ?? username}' logged out from the application.",
                IpAddress = ipAddress,
                Timestamp = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}
