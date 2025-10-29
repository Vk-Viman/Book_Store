using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;

public class OrdersControllerConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public OrdersControllerConcurrencyTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ConcurrentCheckouts_DoNotOversell()
    {
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        // login both clients with the same demo user
        var token = await LoginAsync(client1);
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // pick a product and set its stock to 1
        var prods = await client1.GetFromJsonAsync<JsonElement>("/api/products");
        var first = prods.GetProperty("items").EnumerateArray().First();
        var id = first.GetProperty("id").GetInt32();

        // ensure cart emptied
        await client1.DeleteAsync($"/api/cart/{id}");
        await client2.DeleteAsync($"/api/cart/{id}");

        // both add the same product
        await client1.PostAsync($"/api/cart/{id}?quantity=1", null);
        await client2.PostAsync($"/api/cart/{id}?quantity=1", null);

        // run both checkouts in parallel
        var t1 = client1.PostAsync("/api/orders/checkout", null);
        var t2 = client2.PostAsync("/api/orders/checkout", null);

        await Task.WhenAll(t1, t2);

        var results = new[] { t1.Result, t2.Result };
        // at most one of them should be 200 OK; the other should be 400 due to insufficient stock
        Assert.True(results.Count(r => r.IsSuccessStatusCode) <= 1);
    }

    private async Task<string> LoginAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "user@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }
}
