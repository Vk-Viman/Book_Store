using Microsoft.Extensions.Logging;
using Readify.Data;
using Readify.Models;
using System.Net;
using System.Net.Mail;

namespace Readify.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task SendTemplateAsync(string to, string template, object model);
    }

    public class SmtpOptions
    {
        public bool Enabled { get; set; } = false;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public string From { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = "Readify";
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpOptions _options;
        private readonly AppDbContext? _db;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, Microsoft.Extensions.Options.IOptions<SmtpOptions> options, AppDbContext? db = null)
        {
            _logger = logger;
            _options = options.Value;
            _db = db;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = string.IsNullOrWhiteSpace(_options.Username) ? null : new NetworkCredential(_options.Username, _options.Password)
                };

                var from = new MailAddress(string.IsNullOrWhiteSpace(_options.From) ? _options.Username : _options.From, _options.FromDisplayName);
                var msg = new MailMessage(from, new MailAddress(to))
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(msg);
                _logger.LogInformation("SMTP email sent to {To}: {Subject}", to, subject);
                if (_db != null)
                {
                    _db.EmailLogs.Add(new EmailLog { To = to, Subject = subject, Body = htmlBody, Success = true, Provider = "SMTP" });
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMTP email to {To}", to);
                if (_db != null)
                {
                    _db.EmailLogs.Add(new EmailLog { To = to, Subject = subject, Body = htmlBody, Success = false, Error = ex.Message, Provider = "SMTP" });
                    await _db.SaveChangesAsync();
                }
                throw;
            }
        }

        public Task SendTemplateAsync(string to, string template, object model)
        {
            var (subject, body) = EmailTemplates.Resolve(template, model);
            return SendAsync(to, subject, body);
        }
    }

    public static class EmailTemplates
    {
        public static (string subject, string html) Resolve(string template, object model)
        {
            switch (template)
            {
                case "Welcome":
                    {
                        var name = GetProp(model, "FullName") ?? "there";
                        var subject = "Welcome to Readify";
                        var html = $"<h2>Welcome, {WebUtility.HtmlEncode(name)}!</h2><p>Thanks for joining Readify.</p>";
                        return (subject, html);
                    }
                case "AdminProductCreated":
                    {
                        var title = GetProp(model, "Title") ?? "(no title)";
                        var id = GetProp(model, "Id") ?? "";
                        var subject = "New product created";
                        var html = $"<p>Product created: <strong>{WebUtility.HtmlEncode(title)}</strong> (ID {WebUtility.HtmlEncode(id)})</p>";
                        return (subject, html);
                    }
                case "OrderConfirmation":
                    {
                        var orderId = GetProp(model, "OrderId") ?? "";
                        var subject = $"Your Readify order {orderId}";
                        var html = $"<p>Thanks for your order <strong>{WebUtility.HtmlEncode(orderId)}</strong>.</p>";
                        return (subject, html);
                    }
                default:
                    {
                        var subject = template;
                        var html = $"<p>Template: {WebUtility.HtmlEncode(template)}</p><pre>{System.Text.Json.JsonSerializer.Serialize(model)}</pre>";
                        return (subject, html);
                    }
            }
        }

        private static string? GetProp(object obj, string name)
        {
            var prop = obj.GetType().GetProperty(name);
            var val = prop?.GetValue(obj)?.ToString();
            return val;
        }
    }

    public class LoggingEmailService : IEmailService
    {
        private readonly ILogger<LoggingEmailService> _logger;
        private readonly AppDbContext? _db;

        public LoggingEmailService(ILogger<LoggingEmailService> logger)
        {
            _logger = logger;
        }

        public LoggingEmailService(ILogger<LoggingEmailService> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            _logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, htmlBody);
            if (_db != null)
            {
                _db.EmailLogs.Add(new EmailLog { To = to, Subject = subject, Body = htmlBody, Success = true, Provider = "Log" });
                await _db.SaveChangesAsync();
            }
        }

        public Task SendTemplateAsync(string to, string template, object model)
        {
            var (subject, html) = EmailTemplates.Resolve(template, model);
            return SendAsync(to, subject, html);
        }
    }
}
