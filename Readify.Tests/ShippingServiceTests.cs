using Microsoft.Extensions.Options;
using Readify.Services;
using Xunit;

public class ShippingServiceTests
{
    [Fact]
    public async Task ReturnsLocalRate_ForLocalRegion()
    {
        var opts = Options.Create(new ShippingOptions { Local = 3.5m, National = 6m, International = 20m, FreeShippingThreshold = 100m });
        var svc = new ShippingService(opts);

        var rate = await svc.GetRateAsync("local", 10m);
        Assert.Equal(3.5m, rate);
    }

    [Fact]
    public async Task ReturnsNationalRate_ForUnknownRegionDefaultsToNational()
    {
        var opts = Options.Create(new ShippingOptions { Local = 2m, National = 5m, International = 15m, FreeShippingThreshold = 100m });
        var svc = new ShippingService(opts);

        var rate = await svc.GetRateAsync(null, 10m);
        Assert.Equal(5m, rate);
    }

    [Fact]
    public async Task ReturnsZero_WhenSubtotalExceedsFreeShippingThreshold()
    {
        var opts = Options.Create(new ShippingOptions { Local = 2m, National = 5m, International = 15m, FreeShippingThreshold = 50m });
        var svc = new ShippingService(opts);

        var rate = await svc.GetRateAsync("international", 100m);
        Assert.Equal(0m, rate);
    }

    [Theory]
    [InlineData("local", 2.0)]
    [InlineData("national", 5.0)]
    [InlineData("international", 15.0)]
    public async Task HandlesConfiguredRates(string region, decimal expected)
    {
        var opts = Options.Create(new ShippingOptions { Local = 2m, National = 5m, International = 15m, FreeShippingThreshold = 100m });
        var svc = new ShippingService(opts);
        var rate = await svc.GetRateAsync(region, 0m);
        Assert.Equal(expected, rate);
    }
}
