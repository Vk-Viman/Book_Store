using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

public class ResponseCachingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ResponseCachingTests(WebApplicationFactory<Program> factory) { _factory = factory; }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/books")]
    public async Task Endpoints_ReturnCacheHeaders(string url)
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        // Accept both caching and no-store configs; here we expect max-age>=60 per phase 4
        Assert.True(res.Headers.CacheControl?.MaxAge?.TotalSeconds >= 60);
    }
}
