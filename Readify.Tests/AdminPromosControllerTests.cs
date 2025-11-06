using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Readify.Data;
using Readify.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Linq;

public class AdminPromosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminPromosControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (body.TryGetProperty("token", out var tok)) return tok.GetString() ?? string.Empty;
        return string.Empty;
    }

    [Fact]
    public async Task CreatePromo_RejectsInvalidPercentage()
    {
        var token = await LoginAsAdminAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsJsonAsync("/api/admin/promos", new { Code = "BADPCT", Type = "Percentage", DiscountPercent = 0m, IsActive = true });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreatePromo_AllowsValidFixed()
    {
        var token = await LoginAsAdminAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsJsonAsync("/api/admin/promos", new { Code = "FIX10", Type = "Fixed", FixedAmount = 10.00m, IsActive = true });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task UpdatePromo_RejectsMissingFixedAmount()
    {
        // create promo
        var token = await LoginAsAdminAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/api/admin/promos", new { Code = "TOUPDATE", Type = "Fixed", FixedAmount = 5.00m, IsActive = true });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        // try to update to invalid fixed
        var update = await client.PutAsJsonAsync($"/api/admin/promos/{id}", new { Id = id, Code = "TOUPDATE", Type = "Fixed", FixedAmount = 0.0m, IsActive = true });
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task ListPromos_PagingAndSearch_WorksAndDeleteRemoves()
    {
        var token = await LoginAsAdminAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ensure a set of promos exist
        for (int i = 0; i < 12; i++)
        {
            await client.PostAsJsonAsync("/api/admin/promos", new { Code = $"P{i}", Type = "Fixed", FixedAmount = 1.0m + i, IsActive = true });
        }

        // page 1 pageSize 5
        var page1 = await client.GetFromJsonAsync<JsonElement>("/api/admin/promos?page=1&pageSize=5");
        Assert.True(page1.GetProperty("items").GetArrayLength() == 5);
        var totalPages = page1.GetProperty("totalPages").GetInt32();
        Assert.True(totalPages >= 3);

        // search for P5
        var search = await client.GetFromJsonAsync<JsonElement>("/api/admin/promos?q=P5");
        var found = search.GetProperty("items").EnumerateArray().Any(e => e.GetProperty("code").GetString() == "P5");
        Assert.True(found);

        // delete one promo
        var items = page1.GetProperty("items").EnumerateArray().ToArray();
        var firstId = items[0].GetProperty("id").GetInt32();
        var del = await client.DeleteAsync($"/api/admin/promos/{firstId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // verify deleted
        var verify = await client.GetFromJsonAsync<JsonElement>("/api/admin/promos");
        var still = verify.GetProperty("items").EnumerateArray().Any(e => e.GetProperty("id").GetInt32() == firstId);
        Assert.False(still);
    }
}
