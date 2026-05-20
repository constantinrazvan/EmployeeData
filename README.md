# EmployeeData

A clean, internal HR management tool built with ASP.NET Core MVC. It helps small and medium teams keep track of employees, departments, app access, and onboarding - all in one place, without the overhead of enterprise software.

---
## Purpose

EmployeeData plans to be an open-source tool for small and medium-sized teams that need a simple way to manage employees, departments, onboarding, hosting on-premise and internal application access without the overhead of enterprise HR software.

The project is also meant to serve as a starting point for developers who want to learn, customize, improve, or build their own HR management solution starting from something general and basic.

--

## What it does

- **Employees** - Add, edit, and manage your team. Filter by department, status, or name. Export to CSV whenever you need it.
- **Departments** - Organize your org structure. Each department has a name, a code, a description, and a manager.
- **App Access & Roles** - Control who has access to which internal applications and with what role. Grant or revoke access in seconds, and the employee gets notified by email automatically.
- **Onboarding** - Every new hire gets a default checklist of tasks (email setup, contract signing, equipment allocation, etc.). You can customize it per person and track progress.
- **Reports** - Headcount per department, tenure distribution, turnover rate, hires per month. Useful for a quick HR snapshot.
- **Audit Logs** - Every action taken in the system is logged - who did what, when, and from where. Admin-only.
- **Notifications** - A live badge in the header alerts you to upcoming work anniversaries, pending onboarding tasks, and new hires.
- **Settings** - Configure your database provider (SQLite, PostgreSQL, SQL Server, MySQL), SMTP for email notifications, and email templates - all through the UI.

---

## Getting started

You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download) installed.

```bash
git clone https://github.com/constantinrazvan/EmployeeData
cd EmployeeData
dotnet run
```

That's it. The app creates a SQLite database and seeds it with demo data on first run. Open your browser at `https://localhost:5001`.

**Default credentials:**

| Username | Password | Role          |
|----------|----------|---------------|
| admin    | admin123 | Administrator |
| hr       | hr123    | HR            |

---

## Switching databases

By default the app uses SQLite (zero config). If you want PostgreSQL, SQL Server, or MySQL, go to **Settings → Database** and switch the provider there. No code changes needed.

---

## Email notifications

When you grant or revoke app access, the system sends an email to the employee. If SMTP isn't configured yet, it simulates the email and shows you exactly what would have been sent - so you can test the flow without a real mail server.

Configure your SMTP details in **Settings → SMTP**, and use the **Test Connection** button to verify before saving.

---

## Project structure

```
Controllers/   - One controller per feature (Employees, Departments, Roles, etc.)
Models/        - EF Core entities and settings models
Data/          - DbContext and seed data
Helpers/       - Password hashing, pagination, and settings service
Views/         - Razor views per controller
wwwroot/       - Static assets (CSS, JS)
settings.json  - Runtime config for DB and SMTP (auto-created on first run)
```

---

## Tech stack
- **ASP.NET Core MVC** (.NET 8)
- **Entity Framework Core** with support for SQLite, PostgreSQL, SQL Server, and MySQL
- **Session-based authentication** (no Identity framework, keeps things simple)
- **SHA-256** password hashing
- **Bootstrap 5** for the UI
