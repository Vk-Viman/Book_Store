using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Readify.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class EmailTriggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public EmailTriggerTests(WebApplicationFactory<Program> factory) { _factory = factory; }

    [Fact]
    public async Task Register_WritesEmailLog()
    {
        var client = _factory.CreateClient();
        var email = $"user{System.Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", email, password = "Readify#1234" });
        register.EnsureSuccessStatusCode();

        // give background email logging a short moment (logging impl is awaited, but just in case)
        await Task.Delay(100);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.EmailLogs.AnyAsync(e => e.To == email);
        Assert.True(exists);
    }
}
