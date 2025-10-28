using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Readify.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class ProfileAuditTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ProfileAuditTests(WebApplicationFactory<Program> factory) { _factory = factory; }

    [Fact]
    public async Task UpdateProfile_WritesUserProfileUpdate()
    {
        var client = _factory.CreateClient();
        // login with seeded admin to get JWT
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@readify.local", password = "Readify#Admin123!" });
        login.EnsureSuccessStatusCode();
        var obj = await login.Content.ReadFromJsonAsync<dynamic>();
        var token = (string)obj?.token;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var newName = "Updated Admin";
        var newEmail = "admin+updated@readify.local";
        var res = await client.PutAsJsonAsync("/api/users/me", new { fullName = newName, email = newEmail });
        res.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var last = await db.UserProfileUpdates.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        Assert.NotNull(last);
        Assert.Equal(newName, last!.NewFullName);
        Assert.Equal(newEmail, last.NewEmail);
    }
}
