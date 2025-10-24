using Microsoft.Extensions.Logging;

namespace Readify.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }

    public class LoggingEmailService : IEmailService
    {
        private readonly ILogger<LoggingEmailService> _logger;

        public LoggingEmailService(ILogger<LoggingEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string htmlBody)
        {
            _logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
