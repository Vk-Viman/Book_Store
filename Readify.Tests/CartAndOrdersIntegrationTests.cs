using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;

public class CartAndOrdersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartAndOrdersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> LoginAsync(string email = "user@demo.com", string password = "Readify#Demo123!")
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (body.TryGetProperty("token", out var tok)) return tok.GetString() ?? string.Empty;
        return string.Empty;
    }

    [Fact]
    public async Task AddToCart_IncrementsQuantity_And_Checkout_CreatesOrder_And_ClearsCart()
    {
        var token = await LoginAsync();
        Assert.False(string.IsNullOrEmpty(token));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // get a product
        var productsRes = await client.GetAsync("/api/products");
        productsRes.EnsureSuccessStatusCode();
        var productsJson = await productsRes.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(productsJson).RootElement;
        var items = root.GetProperty("items").EnumerateArray();
        Assert.True(items.MoveNext());
        var first = items.Current;
        var productId = first.GetProperty("id").GetInt32();
        var price = first.GetProperty("price").GetDecimal();

        // ensure cart empty by removing any existing items
        var cartBefore = await client.GetFromJsonAsync<JsonElement[]>"/api/cart";
        if (cartBefore != null)
        {
            foreach (var it in cartBefore)
            {
                var pid = it.GetProperty("productId").GetInt32();
                await client.DeleteAsync($"/api/cart/{pid}");
            }
        }

        // add quantity 1
        var addRes1 = await client.PostAsync($"/api/cart/{productId}?quantity=1", null);
        Assert.Equal(HttpStatusCode.OK, addRes1.StatusCode);

        // add again quantity 2 -> total should be 3
        var addRes2 = await client.PostAsync($"/api/cart/{productId}?quantity=2", null);
        Assert.Equal(HttpStatusCode.OK, addRes2.StatusCode);

        // get cart and verify quantity
        var cart = await client.GetFromJsonAsync<JsonElement[]>("/api/cart");
        Assert.NotNull(cart);
        Assert.Contains(cart, c => c.GetProperty("productId").GetInt32() == productId && c.GetProperty("quantity").GetInt32() == 3);

        // checkout
        var checkoutRes = await client.PostAsync("/api/orders/checkout", null);
        Assert.Equal(HttpStatusCode.OK, checkoutRes.StatusCode);
        var order = await checkoutRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(order.GetProperty("items").GetArrayLength() > 0);
        var total = order.GetProperty("totalAmount").GetDecimal();
        Assert.Equal(price * 3, total);

        // cart should be empty now
        var cartAfter = await client.GetFromJsonAsync<JsonElement[]>("/api/cart");
        Assert.True(cartAfter == null || cartAfter.Length == 0);

        // checkout again should fail (empty cart)
        var checkoutRes2 = await client.PostAsync("/api/orders/checkout", null);
        Assert.Equal(HttpStatusCode.BadRequest, checkoutRes2.StatusCode);
    }

    [Fact]
    public async Task RemoveFromCart_Nonexistent_ReturnsNotFound()
    {
        var token = await LoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // pick unlikely product id
        var res = await client.DeleteAsync($"/api/cart/{int.MaxValue}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
