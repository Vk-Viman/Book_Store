using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Readify.Services;

namespace Readify.Controllers
{
    [Route("api/upload")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _upload;

        public UploadController(IUploadService upload)
        {
            _upload = upload;
        }

        [HttpPost("image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, CancellationToken ct)
        {
            try
            {
                var url = await _upload.SaveImageAsync(file, ct);
                return Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
