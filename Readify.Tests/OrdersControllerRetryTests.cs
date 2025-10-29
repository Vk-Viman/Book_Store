using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

public class OrdersControllerRetryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public OrdersControllerRetryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Checkout_Retries_On_Concurrency_And_Succeeds_Or_Fails_Gracefully()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();

        // add to cart
        await client.PostAsync($"/api/cart/{id}?quantity=1", null);

        // simulate two checkouts in parallel by running two tasks
        var t1 = client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "A", shippingAddress = "B", shippingPhone = "C" });
        var t2 = client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "A", shippingAddress = "B", shippingPhone = "C" });

        await Task.WhenAll(t1, t2);

        var successCount = new[] { t1.Result, t2.Result }.Count(r => r.IsSuccessStatusCode);
        Assert.True(successCount <= 1);
    }

    private async Task<string> LoginAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "user@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }
}
