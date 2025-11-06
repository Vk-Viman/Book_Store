using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

public class AdminUsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AdminUsersControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AdminCanListUsers()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdmin(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/api/admin/users");
        resp.EnsureSuccessStatusCode();
        var users = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(users.ValueKind == JsonValueKind.Array || users.ValueKind == JsonValueKind.Object);
    }

    private async Task<string> LoginAsAdmin(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }
}
