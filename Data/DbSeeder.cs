using System;
using System.Linq;
using EmployeeData.Models;
using EmployeeData.Helpers;

namespace EmployeeData.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.HashPassword("admin123"),
                    FullName = "Alex Popescu",
                    Role = "Administrator",
                    CreatedAt = DateTime.Now.AddDays(-30)
                },
                new User
                {
                    Username = "hr",
                    PasswordHash = PasswordHasher.HashPassword("hr123"),
                    FullName = "Elena Ionescu",
                    Role = "HR",
                    CreatedAt = DateTime.Now.AddDays(-28)
                }
            );
            context.SaveChanges();
        }

        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department
                {
                    Name = "IT Software & Security",
                    Code = "IT",
                    Description = "Application development, Cloud infrastructure, cyber security, and DevOps.",
                    ManagerName = "Dan Radu"
                },
                new Department
                {
                    Name = "Human Resources",
                    Code = "HR",
                    Description = "Talent recruitment, personnel administration, performance evaluation, and training.",
                    ManagerName = "Elena Ionescu"
                },
                new Department
                {
                    Name = "Finance & Accounting",
                    Code = "FIN",
                    Description = "Budget planning, payroll, financial auditing, and cost management.",
                    ManagerName = "Mihai Stoica"
                },
                new Department
                {
                    Name = "Marketing & Public Relations",
                    Code = "MKT",
                    Description = "Brand image, promotional campaigns, SEO, and corporate events organization.",
                    ManagerName = "Laura Georgescu"
                }
            );
            context.SaveChanges();
        }

        var itDept = context.Departments.FirstOrDefault(d => d.Code == "IT");
        var hrDept = context.Departments.FirstOrDefault(d => d.Code == "HR");
        var finDept = context.Departments.FirstOrDefault(d => d.Code == "FIN");
        var mktDept = context.Departments.FirstOrDefault(d => d.Code == "MKT");

        if (!context.Persons.Any() && itDept != null && hrDept != null && finDept != null && mktDept != null)
        {
            context.Persons.AddRange(
                new Person
                {
                    FirstName = "Andrei",
                    LastName = "Nistor",
                    Email = "andrei.nistor@organization.com",
                    Phone = "0722111222",
                    Position = "Senior Software Engineer",
                    DepartmentId = itDept.Id,
                    HireDate = DateTime.Parse("2021-03-15"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Maria",
                    LastName = "Constantinescu",
                    Email = "maria.c@organization.com",
                    Phone = "0733444555",
                    Position = "DevOps Specialist",
                    DepartmentId = itDept.Id,
                    HireDate = DateTime.Parse("2022-09-01"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Vlad",
                    LastName = "Marinescu",
                    Email = "vlad.marinescu@organization.com",
                    Phone = "0744555666",
                    Position = "QA Automation Engineer",
                    DepartmentId = itDept.Id,
                    HireDate = DateTime.Parse("2023-05-10"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Simona",
                    LastName = "Bratu",
                    Email = "simona.bratu@organization.com",
                    Phone = "0755666777",
                    Position = "HR Specialist",
                    DepartmentId = hrDept.Id,
                    HireDate = DateTime.Parse("2020-01-15"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Cristian",
                    LastName = "Sandu",
                    Email = "cristian.sandu@organization.com",
                    Phone = "0766777888",
                    Position = "Technical Recruiter",
                    DepartmentId = hrDept.Id,
                    HireDate = DateTime.Parse("2022-11-20"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Ioana",
                    LastName = "Diaconescu",
                    Email = "ioana.diaconescu@organization.com",
                    Phone = "0722888999",
                    Position = "Chief Accountant",
                    DepartmentId = finDept.Id,
                    HireDate = DateTime.Parse("2018-05-01"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "George",
                    LastName = "Popa",
                    Email = "george.popa@organization.com",
                    Phone = "0733999000",
                    Position = "Financial Analyst",
                    DepartmentId = finDept.Id,
                    HireDate = DateTime.Parse("2024-02-15"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Ana Maria",
                    LastName = "Lazar",
                    Email = "anamaria.lazar@organization.com",
                    Phone = "0744111333",
                    Position = "Brand & PR Manager",
                    DepartmentId = mktDept.Id,
                    HireDate = DateTime.Parse("2021-08-20"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Robert",
                    LastName = "Dumitru",
                    Email = "robert.d@organization.com",
                    Phone = "0755222444",
                    Position = "Lead Graphic Designer",
                    DepartmentId = mktDept.Id,
                    HireDate = DateTime.Parse("2023-01-10"),
                    Status = "Active"
                },
                new Person
                {
                    FirstName = "Mihai",
                    LastName = "Avram",
                    Email = "mihai.avram@organization.com",
                    Phone = "0766333555",
                    Position = "Content Writer Intern",
                    DepartmentId = mktDept.Id,
                    HireDate = DateTime.Parse("2025-02-01"),
                    Status = "Inactive"
                }
            );
            context.SaveChanges();
        }

        if (!context.Applications.Any())
        {
            context.Applications.AddRange(
                new Application
                {
                    Name = "Jira Software Cloud",
                    Description = "Agile project management, sprint planning, and development task tracking.",
                    Url = "https://jira.organization.local"
                },
                new Application
                {
                    Name = "GitLab Enterprise Edition",
                    Description = "Source code hosting, code review (Merge Requests), CI/CD pipelines, and container registry.",
                    Url = "https://gitlab.organization.local"
                },
                new Application
                {
                    Name = "Salesforce CRM Hub",
                    Description = "Customer relationship management, sales opportunities, and marketing integrations.",
                    Url = "https://salesforce.organization.local"
                },
                new Application
                {
                    Name = "Slack Workspace",
                    Description = "Real-time internal communication platform, departmental channels, and automated integrations.",
                    Url = "https://slack.organization.local"
                }
            );
            context.SaveChanges();
        }

        var pAndrei = context.Persons.FirstOrDefault(p => p.Email.StartsWith("andrei.nistor"));
        var pMaria = context.Persons.FirstOrDefault(p => p.Email.StartsWith("maria.c"));
        var pVlad = context.Persons.FirstOrDefault(p => p.Email.StartsWith("vlad.marinescu"));
        var pIoana = context.Persons.FirstOrDefault(p => p.Email.StartsWith("ioana.diaconescu"));
        var pAna = context.Persons.FirstOrDefault(p => p.Email.StartsWith("anamaria.lazar"));

        var appJira = context.Applications.FirstOrDefault(a => a.Name.StartsWith("Jira"));
        var appGitLab = context.Applications.FirstOrDefault(a => a.Name.StartsWith("GitLab"));
        var appSalesforce = context.Applications.FirstOrDefault(a => a.Name.StartsWith("Salesforce"));
        var appSlack = context.Applications.FirstOrDefault(a => a.Name.StartsWith("Slack"));

        if (!context.AppAccessRoles.Any() && appJira != null && appGitLab != null && appSalesforce != null && appSlack != null)
        {
            if (pAndrei != null)
            {
                context.AppAccessRoles.AddRange(
                    new AppAccessRole
                    {
                        PersonId = pAndrei.Id,
                        ApplicationId = appGitLab.Id,
                        RoleName = "Developer",
                        GrantedAt = DateTime.Now.AddDays(-15),
                        GrantedBy = "admin"
                    },
                    new AppAccessRole
                    {
                        PersonId = pAndrei.Id,
                        ApplicationId = appSlack.Id,
                        RoleName = "User",
                        GrantedAt = DateTime.Now.AddDays(-15),
                        GrantedBy = "admin"
                    }
                );
            }

            if (pMaria != null)
            {
                context.AppAccessRoles.AddRange(
                    new AppAccessRole
                    {
                        PersonId = pMaria.Id,
                        ApplicationId = appGitLab.Id,
                        RoleName = "Administrator",
                        GrantedAt = DateTime.Now.AddDays(-12),
                        GrantedBy = "admin"
                    },
                    new AppAccessRole
                    {
                        PersonId = pMaria.Id,
                        ApplicationId = appJira.Id,
                        RoleName = "Editor",
                        GrantedAt = DateTime.Now.AddDays(-12),
                        GrantedBy = "admin"
                    },
                    new AppAccessRole
                    {
                        PersonId = pMaria.Id,
                        ApplicationId = appSlack.Id,
                        RoleName = "User",
                        GrantedAt = DateTime.Now.AddDays(-12),
                        GrantedBy = "admin"
                    }
                );
            }

            if (pVlad != null)
            {
                context.AppAccessRoles.AddRange(
                    new AppAccessRole
                    {
                        PersonId = pVlad.Id,
                        ApplicationId = appGitLab.Id,
                        RoleName = "QA Automation",
                        GrantedAt = DateTime.Now.AddDays(-8),
                        GrantedBy = "admin"
                    },
                    new AppAccessRole
                    {
                        PersonId = pVlad.Id,
                        ApplicationId = appJira.Id,
                        RoleName = "QA Tester",
                        GrantedAt = DateTime.Now.AddDays(-8),
                        GrantedBy = "admin"
                    }
                );
            }

            if (pIoana != null)
            {
                context.AppAccessRoles.AddRange(
                    new AppAccessRole
                    {
                        PersonId = pIoana.Id,
                        ApplicationId = appSalesforce.Id,
                        RoleName = "Viewer",
                        GrantedAt = DateTime.Now.AddDays(-20),
                        GrantedBy = "admin"
                    },
                    new AppAccessRole
                    {
                        PersonId = pIoana.Id,
                        ApplicationId = appSlack.Id,
                        RoleName = "User",
                        GrantedAt = DateTime.Now.AddDays(-20),
                        GrantedBy = "admin"
                    }
                );
            }

            if (pAna != null)
            {
                context.AppAccessRoles.AddRange(
                    new AppAccessRole
                    {
                        PersonId = pAna.Id,
                        ApplicationId = appSlack.Id,
                        RoleName = "Communications Lead",
                        GrantedAt = DateTime.Now.AddDays(-5),
                        GrantedBy = "hr"
                    }
                );
            }

            context.SaveChanges();
        }

        if (!context.AuditLogs.Any())
        {
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    Username = "System",
                    Action = "Initialization",
                    Details = "The database was successfully initialized and populated with basic demo data.",
                    IpAddress = "127.0.0.1",
                    Timestamp = DateTime.Now.AddDays(-30)
                },
                new AuditLog
                {
                    Username = "admin",
                    Action = "Login",
                    Details = "User 'admin' successfully logged into the portal.",
                    IpAddress = "127.0.0.1",
                    Timestamp = DateTime.Now.AddMinutes(-45)
                },
                new AuditLog
                {
                    Username = "admin",
                    Action = "View",
                    Details = "Viewed the complete list of employees in the organization.",
                    IpAddress = "127.0.0.1",
                    Timestamp = DateTime.Now.AddMinutes(-30)
                }
            );
            context.SaveChanges();
        }

        if (!context.OnboardingTasks.Any())
        {
            var persons = context.Persons.ToList();
            foreach (var person in persons)
            {
                var tasks = EmployeeData.Controllers.OnboardingController.CreateDefaultTasks(person.Id);
                var daysSinceHire = (DateTime.Now - person.HireDate).TotalDays;
                if (daysSinceHire > 60)
                {
                    foreach (var t in tasks)
                    {
                        t.IsCompleted = true;
                        t.CompletedAt = person.HireDate.AddDays(14);
                        t.CompletedBy = "admin";
                    }
                }
                else if (daysSinceHire > 14)
                {
                    for (int i = 0; i < Math.Min(5, tasks.Count); i++)
                    {
                        tasks[i].IsCompleted = true;
                        tasks[i].CompletedAt = person.HireDate.AddDays(7);
                        tasks[i].CompletedBy = "hr";
                    }
                }
                context.OnboardingTasks.AddRange(tasks);
            }
            context.SaveChanges();
        }
    }
}
