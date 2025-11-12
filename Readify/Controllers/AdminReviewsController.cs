using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;

namespace Readify.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminReviewsController> _logger;

    public AdminReviewsController(AppDbContext db, ILogger<AdminReviewsController> logger)
    {
        _db = db; _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPending()
    {
        var list = await _db.Reviews.Where(r => !r.IsApproved).OrderBy(r => r.CreatedAt).ToListAsync();
        return Ok(list);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] bool approve)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        r.IsApproved = approve;
        await _db.SaveChangesAsync();
        return Ok(r);
    }
}
