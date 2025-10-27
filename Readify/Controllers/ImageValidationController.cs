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
            if (req == null || string.IsNullOrWhiteSpace(req.Url)) return BadRequest(new { ok = false, message = "url is required" });

            if (!Uri.TryCreate(req.Url, UriKind.Absolute, out var uri))
            {
                return BadRequest(new { ok = false, message = "Invalid URL format" });
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return BadRequest(new { ok = false, message = "Only HTTP/HTTPS URLs are allowed" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);

                // Use HEAD first to minimize download
                try
                {
                    using var headReq = new HttpRequestMessage(HttpMethod.Head, req.Url);
                    var headResp = await client.SendAsync(headReq);
                    if (headResp.IsSuccessStatusCode)
                    {
                        var mediaType = headResp.Content?.Headers?.ContentType?.MediaType;
                        if (!string.IsNullOrEmpty(mediaType) && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                            return Ok(new { ok = true });
                        _logger.LogInformation("HEAD returned non-image media type {MediaType} for {Url}", mediaType, req.Url);
                    }
                }
                catch (HttpRequestException hre)
                {
                    _logger.LogInformation(hre, "HEAD request failed for {Url}, will try GET fallback", req.Url);
                }

                // Fallback: GET but only read headers (range 0-0)
                try
                {
                    using var getReq = new HttpRequestMessage(HttpMethod.Get, req.Url);
                    getReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                    var getResp = await client.SendAsync(getReq);
                    var getMediaType = getResp.Content?.Headers?.ContentType?.MediaType;
                    if (getResp.IsSuccessStatusCode && !string.IsNullOrEmpty(getMediaType) && getMediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        return Ok(new { ok = true });
                    }

                    _logger.LogWarning("Image validation failed for {Url}: status {Status}, mediaType {MediaType}", req.Url, (int)getResp.StatusCode, getMediaType);
                    return BadRequest(new { ok = false, message = "URL did not return an image content-type", status = (int)getResp.StatusCode, mediaType = getMediaType });
                }
                catch (HttpRequestException hre)
                {
                    _logger.LogWarning(hre, "GET fallback failed for {Url}", req.Url);
                    return StatusCode(502, new { ok = false, message = "Failed to reach the URL", detail = hre.Message });
                }
            }
            catch (TaskCanceledException tce)
            {
                _logger.LogWarning(tce, "Image validation timed out for {Url}", req.Url);
                return StatusCode(504, new { ok = false, message = "Image validation timed out", detail = tce.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while validating image {Url}", req.Url);
                // Include exception message for debugging in Development
                return StatusCode(500, new { ok = false, message = "Failed to validate image", detail = ex.Message });
            }
        }
    }

    public class ImageValidationRequest { public string Url { get; set; } = string.Empty; }
}
