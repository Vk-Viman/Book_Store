using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

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
}
