using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

public class OrdersControllerPaymentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public OrdersControllerPaymentTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Checkout_Fails_When_PaymentTokenIndicatesFailure()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Payment-Token", "fail-token");

        // ensure user's cart has at least one item; add a product
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();
        await client.PostAsync($"/api/cart/{id}?quantity=1", null);

        var resp = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "T", shippingAddress = "A", shippingPhone = "P" });
        Assert.False(resp.IsSuccessStatusCode);
    }

    private async Task<string> LoginAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "user@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }
}
