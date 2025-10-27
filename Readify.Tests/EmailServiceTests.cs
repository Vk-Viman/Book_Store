using Microsoft.Extensions.Logging;
using Readify.Services;
using System.Threading.Tasks;
using Xunit;

public class EmailServiceTests
{
    [Fact]
    public async Task LoggingEmailService_SendAsync_Completes()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<LoggingEmailService>();
        var svc = new LoggingEmailService(logger);

        await svc.SendAsync("test@example.com", "Subject", "<p>Body</p>");

        // If no exception thrown and task completes, consider success
        Assert.True(true);
    }
}
