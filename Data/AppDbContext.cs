using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmployeeData.Models;

namespace EmployeeData.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<AppAccessRole> AppAccessRoles => Set<AppAccessRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>()
            .HasOne(p => p.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppAccessRole>()
            .HasOne(ar => ar.Person)
            .WithMany(p => p.AppAccessRoles)
            .HasForeignKey(ar => ar.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppAccessRole>()
            .HasOne(ar => ar.Application)
            .WithMany(a => a.AppAccessRoles)
            .HasForeignKey(ar => ar.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OnboardingTask>()
            .HasOne(ot => ot.Person)
            .WithMany()
            .HasForeignKey(ot => ot.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
