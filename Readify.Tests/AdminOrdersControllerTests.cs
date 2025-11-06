using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

public class AdminOrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AdminOrdersControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> LoginAsAdmin(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@demo.com", password = "Readify#Demo123!" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString() ?? string.Empty;
    }

    [Fact]
    public async Task Admin_Can_Update_Order_Status_And_Email_Sent()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsAdmin(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // create an order via user flow
        var userLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = "user@demo.com", password = "Readify#Demo123!" });
        userLogin.EnsureSuccessStatusCode();
        var userBody = await userLogin.Content.ReadFromJsonAsync<JsonElement>();
        var userToken = userBody.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // ensure cart has at least one item and create order
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.GetProperty("items").EnumerateArray().First().GetProperty("id").GetInt32();
        await client.PostAsync($"/api/cart/{id}?quantity=1", null);
        var resp = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingName = "T", shippingAddress = "A", shippingPhone = "P" });
        resp.EnsureSuccessStatusCode();
        var order = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetInt32();

        // admin update status
        var adminClient = _factory.CreateClient();
        var adminToken = await LoginAsAdmin(adminClient);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var update = await adminClient.PutAsJsonAsync($"/api/admin/orders/update-status/{orderId}", new { orderStatus = "Shipped" });
        update.EnsureSuccessStatusCode();

        // verify email log exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Readify.Data.AppDbContext>();
        var exists = await db.EmailLogs.AnyAsync(e => e.Subject.Contains(orderId.ToString()));
        Assert.True(exists);
    }
}
