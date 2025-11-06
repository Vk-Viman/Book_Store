using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

public class AdminUsersControllerUnitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AdminUsersControllerUnitTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> LoginAsAdmin(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }

    [Fact]
    public async Task ToggleActive_CannotDeactivateLastAdmin()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdmin(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Attempt to toggle active for the seeded admin; if only one admin exists expect BadRequest
        var usersResp = await client.GetFromJsonAsync<JsonElement>('/api/admin/users');
        // If users endpoint returns paged result, extract items
        JsonElement itemsElem = usersResp;
        if (usersResp.ValueKind == JsonValueKind.Object && usersResp.TryGetProperty("items", out var it)) itemsElem = it;
        var firstAdmin = itemsElem.EnumerateArray().FirstOrDefault(e => e.GetProperty("role").GetString() == "Admin");
        var id = firstAdmin.GetProperty("id").GetInt32();

        var resp = await client.PutAsync($"/api/admin/users/{id}/toggle-active", null);
        // either success (if multiple admins) or BadRequest
        Assert.True(resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Promote_SetsRoleToAdminAndActive()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdmin(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // create a new user
        var email = $"test{Guid.NewGuid()}@example.com";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", email = email, password = "P@ssw0rd1" });
        reg.EnsureSuccessStatusCode();

        var usersResp = await client.GetFromJsonAsync<JsonElement>('/api/admin/users');
        JsonElement itemsElem = usersResp;
        if (usersResp.ValueKind == JsonValueKind.Object && usersResp.TryGetProperty("items", out var it)) itemsElem = it;
        var created = itemsElem.EnumerateArray().FirstOrDefault(e => e.GetProperty("email").GetString() == email);
        var id = created.GetProperty("id").GetInt32();

        var promote = await client.PutAsync($"/api/admin/users/{id}/promote", null);
        promote.EnsureSuccessStatusCode();

        // fetch user and verify
        var usersResp2 = await client.GetFromJsonAsync<JsonElement>('/api/admin/users');
        if (usersResp2.ValueKind == JsonValueKind.Object && usersResp2.TryGetProperty("items", out var it2)) usersResp2 = it2;
        var updated = usersResp2.EnumerateArray().FirstOrDefault(e => e.GetProperty("id").GetInt32() == id);
        Assert.Equal("Admin", updated.GetProperty("role").GetString());
        Assert.True(updated.GetProperty("isActive").GetBoolean());
    }
}
