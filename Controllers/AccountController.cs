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
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            _context.AuditLogs.Add(new AuditLog
            {
                Username = model.Username,
                Action = "Login",
                Details = $"User '{user?.FullName}' successfully logged in.",
                IpAddress = ipAddress,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Employees");
        }

        ModelState.AddModelError("", "Incorrect username or password.");

        _context.AuditLogs.Add(new AuditLog
        {
            Username = model.Username,
            Action = "Login Failure",
            Details = $"Failed login attempt for username '{model.Username}'.",
            IpAddress = ipAddress,
            Timestamp = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return View(model);
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
