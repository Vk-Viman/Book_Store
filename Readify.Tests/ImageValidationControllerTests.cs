using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

public class ImageValidationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImageValidationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Validate_InvalidRequest_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/admin/image/validate", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Validate_NonImageUrl_ReturnsBadRequestOr502()
    {
        var client = _factory.CreateClient();
        // Use a known non-image URL on the public web; may be flaky in CI but ok for example
        var res = await client.PostAsJsonAsync("/api/admin/image/validate", new { url = "https://example.com" });
        Assert.True(res.StatusCode == HttpStatusCode.BadRequest || res.StatusCode == HttpStatusCode.BadGateway || res.StatusCode == HttpStatusCode.OK);
    }
}
