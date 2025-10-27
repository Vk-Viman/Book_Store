using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class AdminProductsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminProductsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var client = _factory.CreateClient();
        var login = new { email = "admin@readify.local", password = "Readify#Admin123!" };
        var res = await client.PostAsJsonAsync("/api/auth/login", login);
        res.EnsureSuccessStatusCode();
        var obj = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (obj.TryGetProperty("token", out var tok)) return tok.GetString() ?? string.Empty;
        return string.Empty;
    }

    [Fact]
    public async Task Admin_CRUD_Product_Works()
    {
        var token = await GetAdminTokenAsync();
        Assert.False(string.IsNullOrEmpty(token));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // pick a category
        var cats = await client.GetFromJsonAsync<JsonElement[]>("/api/categories");
        int categoryId = 1;
        if (cats != null && cats.Length > 0)
        {
            if (cats[0].TryGetProperty("id", out var idProp)) categoryId = idProp.GetInt32();
        }

        var product = new
        {
            title = "Integration Test Product",
            authors = "Test Author",
            description = "",
            price = 1.23m,
            stockQty = 5,
            categoryId = categoryId,
            imageUrl = "https://via.placeholder.com/150"
        };

        // Create
        var createRes = await client.PostAsJsonAsync("/api/admin/products", product);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        // Read
        var getRes = await client.GetAsync($"/api/admin/products/{id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        // Update
        var updated = new
        {
            id = id,
            title = "Integration Test Product Updated",
            authors = "Test Author",
            description = "Updated",
            price = 2.34m,
            stockQty = 10,
            categoryId = categoryId,
            imageUrl = "https://via.placeholder.com/150"
        };
        var putRes = await client.PutAsJsonAsync($"/api/admin/products/{id}", updated);
        Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

        // Delete
        var delRes = await client.DeleteAsync($"/api/admin/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidProduct_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidProduct = new
        {
            title = "", // missing title should trigger validation
            authors = "Test",
            price = 1.0m,
            stockQty = 1,
            categoryId = 1,
            imageUrl = "https://via.placeholder.com/150"
        };

        var res = await client.PostAsJsonAsync("/api/admin/products", invalidProduct);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        var token = await GetAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var product = new
        {
            title = "Product With Bad Category",
            authors = "Test",
            price = 1.0m,
            stockQty = 1,
            categoryId = 999999, // unlikely to exist
            imageUrl = "https://via.placeholder.com/150"
        };

        var res = await client.PostAsJsonAsync("/api/admin/products", product);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Create_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var product = new
        {
            title = "Unauthorized Product",
            authors = "Test",
            price = 1.0m,
            stockQty = 1,
            categoryId = 1,
            imageUrl = "https://via.placeholder.com/150"
        };

        var res = await client.PostAsJsonAsync("/api/admin/products", product);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Get_NonexistentProduct_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/admin/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Update_NonexistentProduct_ReturnsNotFound()
    {
        var token = await GetAdminTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updated = new
        {
            id = 999999,
            title = "Doesn't Exist",
            authors = "Test",
            description = "",
            price = 1.23m,
            stockQty = 1,
            categoryId = 1,
            imageUrl = "https://via.placeholder.com/150"
        };

        var res = await client.PutAsJsonAsync("/api/admin/products/999999", updated);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
