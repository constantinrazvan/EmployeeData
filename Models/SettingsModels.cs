using System.Collections.Generic;

namespace EmployeeData.Models;

public class DatabaseSettings
{
    public string DbPath { get; set; } = "EmployeeData.db";
    public string Provider { get; set; } = "SQLite";
    public string ConnectionString { get; set; } = "";
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string DatabaseName { get; set; } = "EmployeeDataDb";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = false;
    public string FromEmail { get; set; } = "noreply@organization.local";
    public string FromName { get; set; } = "Organization Portal";
}

public class EmailTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SystemSettings
{
    public DatabaseSettings Database { get; set; } = new DatabaseSettings();
    public SmtpSettings Smtp { get; set; } = new SmtpSettings();
    public List<EmailTemplate> Templates { get; set; } = new List<EmailTemplate>();
}
