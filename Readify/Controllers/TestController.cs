using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;

namespace Readify.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<TestController> _logger;

    public TestController(AppDbContext context, IWebHostEnvironment env, IConfiguration config, ILogger<TestController> logger)
    {
        _context = context;
        _env = env;
        _config = config;
        _logger = logger;
    }

    [HttpPost("reset")] // POST /api/test/reset
    public async Task<IActionResult> ResetDatabase()
    {
        if (!_env.IsDevelopment()) return Forbid();

        try
        {
            _logger.LogInformation("Test reset: ensuring database deleted");
            await _context.Database.EnsureDeletedAsync();
            _logger.LogInformation("Test reset: migrating database");
            await _context.Database.MigrateAsync();

            _logger.LogInformation("Test reset: running DB initializer");
            await DbInitializer.InitializeAsync(_context, _config, _logger, _env);

            // return id of first product for tests
            var prod = await _context.Products.OrderBy(p => p.Id).FirstOrDefaultAsync();
            return Ok(new { productId = prod?.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database for tests");
            return StatusCode(500, new { message = "Failed to reset database", detail = ex.Message });
        }
    }
}
