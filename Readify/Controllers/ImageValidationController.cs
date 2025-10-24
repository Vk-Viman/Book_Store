using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Readify.Controllers
{
    [Route("api/admin/image")]
    [ApiController]
    public class ImageValidationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageValidationController> _logger;

        public ImageValidationController(IHttpClientFactory httpClientFactory, ILogger<ImageValidationController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // POST api/admin/image/validate
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ImageValidationRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Url)) return BadRequest(new { message = "url is required" });

            try
            {
                var client = _httpClientFactory.CreateClient();
                // Use HEAD first to minimize download
                using var headReq = new HttpRequestMessage(HttpMethod.Head, req.Url);
                var headResp = await client.SendAsync(headReq);
                if (headResp.IsSuccessStatusCode)
                {
                    var content = headResp.Content;
                    var mediaType = content?.Headers?.ContentType?.MediaType;
                    if (!string.IsNullOrEmpty(mediaType) && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        return Ok(new { ok = true });
                    _logger.LogInformation("HEAD returned non-image media type {MediaType} for {Url}", mediaType, req.Url);
                }

                // Fallback: GET but only read headers
                using var getReq = new HttpRequestMessage(HttpMethod.Get, req.Url);
                getReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                var getResp = await client.SendAsync(getReq);
                var getMediaType = getResp.Content?.Headers?.ContentType?.MediaType;
                if (getResp.IsSuccessStatusCode && !string.IsNullOrEmpty(getMediaType) && getMediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { ok = true });
                }

                _logger.LogWarning("Image validation failed for {Url}: status {Status}, mediaType {MediaType}", req.Url, (int)getResp.StatusCode, getMediaType);
                return BadRequest(new { ok = false, message = "URL did not return an image content-type" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Image validation failed for {Url}", req?.Url);
                return StatusCode(500, new { ok = false, message = "Failed to validate image" });
            }
        }
    }

    public class ImageValidationRequest { public string Url { get; set; } = string.Empty; }
}
