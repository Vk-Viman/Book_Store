using Microsoft.AspNetCore.Mvc;
using Readify.Services;

namespace Readify.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _ship;

    public ShippingController(IShippingService ship) => _ship = ship;

    [HttpGet("rate")]
    public async Task<IActionResult> GetRate([FromQuery] string? region, [FromQuery] decimal? subtotal)
    {
        var s = await _ship.GetRateAsync(region, subtotal ?? 0m);
        return Ok(new { region = region ?? "national", rate = s });
    }

    public record DetectDto(string? Address);

    [HttpPost("detect")]
    public IActionResult Detect([FromBody] DetectDto dto)
    {
        var addr = (dto?.Address ?? string.Empty).ToLowerInvariant();

        // simple rule-based detection
        if (string.IsNullOrWhiteSpace(addr)) return Ok(new { region = (string?)null });

        // local identifiers (country or major city)
        var localKeywords = new[] { "sri lanka", "colombo", "lk", "sl" };
        if (localKeywords.Any(k => addr.Contains(k)))
        {
            // treat local (same city/area)
            return Ok(new { region = "local" });
        }

        // common international country names -> international
        var internationalKeywords = new[] { "united states", "united kingdom", "usa", "us", "canada", "australia", "india", "china", "germany", "france" };
        if (internationalKeywords.Any(k => addr.Contains(k)))
        {
            return Ok(new { region = "international" });
        }

        // fallback: if address contains a country-like token (comma separated last token length > 2), treat as international
        try
        {
            var parts = addr.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (parts.Length > 1)
            {
                var last = parts.Last();
                if (last.Length > 2 && !localKeywords.Any(k => last.Contains(k)))
                {
                    return Ok(new { region = "international" });
                }
            }
        }
        catch { }

        // otherwise assume national
        return Ok(new { region = "national" });
    }
}
