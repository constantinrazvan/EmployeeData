using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using EmployeeData.Models;

namespace EmployeeData.Helpers;

public static class SettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
    private static SystemSettings _cachedSettings = null!;
    private static readonly object LockObj = new();

    static SettingsService()
    {
        LoadSettings();
    }

    public static SystemSettings GetSettings()
    {
        lock (LockObj)
        {
            if (_cachedSettings == null)
            {
                LoadSettings();
            }
            return _cachedSettings;
        }
    }

    public static void SaveSettings(SystemSettings settings)
    {
        lock (LockObj)
        {
            _cachedSettings = settings;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_cachedSettings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }

    public static string GetConnectionString()
    {
        var settings = GetSettings();
        var provider = settings.Database.Provider ?? "SQLite";

        if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            var path = settings.Database.DbPath;
            if (string.IsNullOrEmpty(path))
            {
                path = "EmployeeData.db";
            }
            return $"Data Source={path}";
        }
        else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            return $"Host={settings.Database.Server};Port={settings.Database.Port};Database={settings.Database.DatabaseName};Username={settings.Database.Username};Password={settings.Database.Password}";
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return $"Server={settings.Database.Server},{settings.Database.Port};Database={settings.Database.DatabaseName};User Id={settings.Database.Username};Password={settings.Database.Password};TrustServerCertificate=True";
        }
        else if (provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
            return $"Server={settings.Database.Server};Port={settings.Database.Port};Database={settings.Database.DatabaseName};Uid={settings.Database.Username};Pwd={settings.Database.Password}";
        }

        return $"Data Source=EmployeeData.db";
    }

    private static void LoadSettings()
    {
        lock (LockObj)
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _cachedSettings = JsonSerializer.Deserialize<SystemSettings>(json) ?? CreateDefaultSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading settings, using defaults: {ex.Message}");
                    _cachedSettings = CreateDefaultSettings();
                }
            }
            else
            {
                _cachedSettings = CreateDefaultSettings();
                SaveSettings(_cachedSettings);
            }
        }
    }

    private static SystemSettings CreateDefaultSettings()
    {
        var settings = new SystemSettings();

        settings.Templates.Add(new EmailTemplate
        {
            Id = "access_granted",
            Name = "Access Granted in Application",
            Subject = "Access Granted to {AppName}",
            Body = "Hello {EmployeeName},\n\nWe confirm that you have been granted access to application '{AppName}' with role '{RoleName}'.\n\nYou can access the application directly at: {AppUrl}.\n\nChange made by: {GrantedBy}.\n\nRegards,\nThe {FromName} Team"
        });

        settings.Templates.Add(new EmailTemplate
        {
            Id = "access_revoked",
            Name = "Access Revoked from Application",
            Subject = "Access Revoked from {AppName}",
            Body = "Hello {EmployeeName},\n\nWe inform you that your access to application '{AppName}' (previous role: '{RoleName}') has been revoked.\n\nIf you believe this is an error, please contact the system administrator or the IT team.\n\nRegards,\nThe {FromName} Team"
        });

        return settings;
    }

    public static (bool Success, string LogMessage) SendNotification(string templateId, Dictionary<string, string> parameters, string toEmail)
    {
        var settings = GetSettings();
        var template = settings.Templates.Find(t => t.Id == templateId);
        if (template == null)
        {
            return (false, $"Template with ID '{templateId}' was not found.");
        }

        var subject = template.Subject;
        var body = template.Body;

        var fromName = settings.Smtp.FromName;
        if (!parameters.ContainsKey("FromName")) parameters["FromName"] = fromName;

        foreach (var param in parameters)
        {
            subject = subject.Replace("{" + param.Key + "}", param.Value);
            body = body.Replace("{" + param.Key + "}", param.Value);
        }

        var smtp = settings.Smtp;
        if (string.IsNullOrEmpty(smtp.Host))
        {
            return (false, "SMTP server is not configured. Host is empty.");
        }

        try
        {
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress(smtp.FromEmail, smtp.FromName);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                using (var client = new SmtpClient(smtp.Host, smtp.Port))
                {
                    client.EnableSsl = smtp.EnableSsl;
                    if (!string.IsNullOrEmpty(smtp.Username) && !string.IsNullOrEmpty(smtp.Password))
                    {
                        client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
                    }

                    client.Timeout = 3000;
                    client.Send(mail);
                }
            }

            return (true, $"Email sent successfully to {toEmail} via SMTP ({smtp.Host}:{smtp.Port}).\nSubject: {subject}");
        }
        catch (Exception ex)
        {
            var debugInfo = $"[Simulated Email Notification]\n" +
                            $"To: {toEmail}\n" +
                            $"From: {smtp.FromName} <{smtp.FromEmail}>\n" +
                            $"Subject: {subject}\n" +
                            $"Body:\n{body}";

            Console.WriteLine(debugInfo);
            return (true, $"Email simulated successfully (Direct connection to {smtp.Host}:{smtp.Port} failed or is not configured). Email details:\n\n{debugInfo}");
        }
    }
}
