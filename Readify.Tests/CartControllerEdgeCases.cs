using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;

public class CartControllerEdgeCases : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartControllerEdgeCases(WebApplicationFactory<Program> factory)
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
    public async Task AddToCart_InvalidQuantity_ReturnsBadRequest()
    {
        var token = await LoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // get a product id
        var productsRes = await client.GetAsync("/api/products");
        productsRes.EnsureSuccessStatusCode();
        var productsJson = await productsRes.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(productsJson).RootElement;
        var items = root.GetProperty("items").EnumerateArray();
        Assert.True(items.MoveNext());
        var first = items.Current;
        var productId = first.GetProperty("id").GetInt32();

        // invalid quantity 0
        var res = await client.PostAsync($"/api/cart/{productId}?quantity=0", null);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task AddToCart_ExceedsStock_ClampsToStock()
    {
        var token = await LoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // get a product with stock
        var productsRes = await client.GetAsync("/api/products");
        productsRes.EnsureSuccessStatusCode();
        var productsJson = await productsRes.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(productsJson).RootElement;
        var items = root.GetProperty("items").EnumerateArray();
        Assert.True(items.MoveNext());
        var first = items.Current;
        var productId = first.GetProperty("id").GetInt32();
        var stock = first.GetProperty("stockQty").GetInt32();

        // ensure cart cleaned
        var cartBefore = await client.GetFromJsonAsync<JsonElement[]>("/api/cart");
        if (cartBefore != null)
        {
            foreach (var it in cartBefore)
            {
                var pid = it.GetProperty("productId").GetInt32();
                await client.DeleteAsync($"/api/cart/{pid}");
            }
        }

        // add more than stock
        var addRes = await client.PostAsync($"/api/cart/{productId}?quantity={stock + 10}", null);
        Assert.Equal(HttpStatusCode.OK, addRes.StatusCode);

        var cart = await client.GetFromJsonAsync<JsonElement[]>("/api/cart");
        Assert.Contains(cart, c => c.GetProperty("productId").GetInt32() == productId && c.GetProperty("quantity").GetInt32() == stock);
    }

    [Fact]
    public async Task AddToCart_NonexistentProduct_ReturnsNotFound()
    {
        var token = await LoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.PostAsync($"/api/cart/{int.MaxValue}?quantity=1", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
