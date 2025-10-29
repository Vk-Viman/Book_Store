using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

public class OrdersControllerEdgeCases : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public OrdersControllerEdgeCases(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Checkout_ReturnsBadRequest_When_InsufficientStock()
    {
        var adminClient = _factory.CreateClient();
        var userClient = _factory.CreateClient();

        var adminToken = await LoginAsync(adminClient, "admin@demo.com");
        var userToken = await LoginAsync(userClient, "user@demo.com");

        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // pick a product
        var prods = await userClient.GetFromJsonAsync<JsonElement>("/api/products");
        var first = prods.GetProperty("items").EnumerateArray().First();
        var id = first.GetProperty("id").GetInt32();

        // set stock to 0 via admin API
        var get = await adminClient.GetFromJsonAsync<JsonElement>($"/api/admin/products/{id}");
        var product = new {
            id = id,
            title = get.GetProperty("title").GetString(),
            description = get.GetProperty("description").GetString(),
            isbn = get.GetProperty("isbn").GetString(),
            authors = get.GetProperty("authors").GetString(),
            publisher = get.GetProperty("publisher").GetString(),
            releaseDate = (string?)null,
            price = get.GetProperty("price").GetDecimal(),
            stockQty = 0,
            categoryId = get.GetProperty("categoryId").GetInt32(),
            imageUrl = get.GetProperty("imageUrl").GetString(),
            language = get.GetProperty("language").GetString(),
            format = get.GetProperty("format").GetString()
        };
        var up = await adminClient.PutAsJsonAsync($"/api/admin/products/{id}", product);
        up.EnsureSuccessStatusCode();

        // user adds to cart (should be limited by stock) then checkout
        await userClient.PostAsync($"/api/cart/{id}?quantity=1", null);
        var resp = await userClient.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "T", shippingAddress = "A", shippingPhone = "P" });
        Assert.False(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Checkout_ReturnsBadRequest_When_ProductMissing()
    {
        var adminClient = _factory.CreateClient();
        var userClient = _factory.CreateClient();

        var adminToken = await LoginAsync(adminClient, "admin@demo.com");
        var userToken = await LoginAsync(userClient, "user@demo.com");

        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // pick a product
        var prods = await userClient.GetFromJsonAsync<JsonElement>("/api/products");
        var first = prods.GetProperty("items").EnumerateArray().First();
        var id = first.GetProperty("id").GetInt32();

        // user adds to cart
        await userClient.PostAsync($"/api/cart/{id}?quantity=1", null);

        // admin deletes the product
        var del = await adminClient.DeleteAsync($"/api/admin/products/{id}");
        del.EnsureSuccessStatusCode();

        // now user tries to checkout
        var resp = await userClient.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "T", shippingAddress = "A", shippingPhone = "P" });
        Assert.False(resp.IsSuccessStatusCode);
    }

    private async Task<string> LoginAsync(HttpClient client, string email)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = email, password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }
}
