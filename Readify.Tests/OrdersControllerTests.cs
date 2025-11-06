using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Headers;
using System.Text.Json;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/orders/checkout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private async Task<string> LoginAsUser(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "user@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }

    [Fact]
    public async Task CreateOrder_FromCart_Succeeds()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsUser(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ensure cart has at least one item
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();
        await client.PostAsync($"/api/cart/{id}?quantity=1", null);

        var resp = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "Test", shippingAddress = "Addr", shippingPhone = "123" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task CancelOrder_OnlyProcessingAllowed()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsUser(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // create order
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();
        await client.PostAsync($"/api/cart/{id}?quantity=1", null);
        var resp = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "Test", shippingAddress = "Addr", shippingPhone = "123" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("id").GetInt32();

        // Attempt cancel should succeed because new orders are Processing
        var del = await client.DeleteAsync($"/api/orders/{orderId}");
        Assert.True(del.IsSuccessStatusCode);
    }
}
