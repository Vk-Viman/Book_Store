using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class BooksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BooksControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBooks_Defaults_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var str = await res.Content.ReadAsStringAsync();
        Assert.Contains("items", str);
    }

    [Fact]
    public async Task Create_Unauthorized_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/books", new { title = "x", authors = "a", categoryId = 1, price = 1.0m, stockQty = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
