using Microsoft.EntityFrameworkCore;
using EmployeeData.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var settings = EmployeeData.Helpers.SettingsService.GetSettings();
    var provider = settings.Database.Provider ?? "SQLite";
    var connString = EmployeeData.Helpers.SettingsService.GetConnectionString();

    switch (provider.ToUpperInvariant())
    {
        case "POSTGRESQL":
        case "POSTGRES":
        case "PGSQL":
            options.UseNpgsql(connString);
            break;
        case "SQLSERVER":
        case "MSSQL":
        case "MS-SQL":
            options.UseSqlServer(connString);
            break;
        case "MYSQL":
        case "MARIADB":
            options.UseMySql(connString, ServerVersion.AutoDetect(connString));
            break;
        case "SQLITE":
        default:
            options.UseSqlite(connString);
            break;
    }
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
