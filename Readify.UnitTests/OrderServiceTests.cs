using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Readify.Data;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http.Json;

public class OrderServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderServiceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => { builder.ConfigureTestServices(services => { /* can mock auth here */ }); });
    }

    [Fact]
    public async Task GetMyOrders_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/orders/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
