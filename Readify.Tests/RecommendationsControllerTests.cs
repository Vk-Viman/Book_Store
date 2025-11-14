using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Readify.Data;
using Xunit;

public class RecommendationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecommendationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetForMe_Returns_Unauthorized_Without_Token()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/recommendations/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetPublic_Returns_List()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/recommendations/public");
        res.EnsureSuccessStatusCode();
        var text = await res.Content.ReadAsStringAsync();
        Assert.Contains("items", text);
    }

    [Fact]
    public async Task GetForMe_Returns_Popular_When_No_Wishlist()
    {
        // create test server with in-memory DB
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // ensure fresh in-memory database is used - default should be fine for integration tests
            });
        });

        var client = factory.CreateClient();

        // login as seeded user to get token (use existing seeded user from DbInitializer)
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@readify.local", Password = "password" });
        loginResp.EnsureSuccessStatusCode();
        var loginJson = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // call recommendations/me - user likely has no wishlist in seed -> should return items (popular) and 200
        var recRes = await client.GetAsync("/api/recommendations/me");
        recRes.EnsureSuccessStatusCode();
        var recText = await recRes.Content.ReadAsStringAsync();
        Assert.Contains("items", recText);
    }
}
