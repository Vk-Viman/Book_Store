using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.Services;

namespace Readify.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/suppliers")]
    public class AdminSuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminSuppliersController> _logger;
        private readonly IAuditService _audit;

        public AdminSuppliersController(AppDbContext context, ILogger<AdminSuppliersController> logger, IAuditService audit)
        {
            _context = context;
            _logger = logger;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return NotFound(new { message = "Supplier not found" });
            return Ok(s);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Supplier model)
        {
            if (model == null) return BadRequest(new { message = "Invalid payload" });
            model.Name = (model.Name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(model.Name)) return BadRequest(new { message = "Name required" });
            model.CreatedAt = DateTime.UtcNow;
            _context.Suppliers.Add(model);
            await _context.SaveChangesAsync();
            try { await _audit.WriteAsync("SupplierCreated", nameof(Supplier), model.Id, $"Name={model.Name}"); } catch { }
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Supplier updated)
        {
            if (updated == null || id != updated.Id) return BadRequest(new { message = "Invalid payload" });
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return NotFound(new { message = "Supplier not found" });
            var old = new { s.Name, s.Email, s.Phone, s.Address, s.IsActive };
            s.Name = (updated.Name ?? string.Empty).Trim();
            s.Email = updated.Email ?? string.Empty;
            s.Phone = updated.Phone ?? string.Empty;
            s.Address = updated.Address ?? string.Empty;
            s.IsActive = updated.IsActive;
            s.UpdatedAt = DateTime.UtcNow;
            _context.Suppliers.Update(s);
            await _context.SaveChangesAsync();
            try { await _audit.WriteAsync("SupplierUpdated", nameof(Supplier), s.Id, System.Text.Json.JsonSerializer.Serialize(new { old, @new = s })); } catch { }
            return Ok(s);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return NotFound();
            // soft-delete: mark inactive
            s.IsActive = false;
            s.UpdatedAt = DateTime.UtcNow;
            _context.Suppliers.Update(s);
            await _context.SaveChangesAsync();
            try { await _audit.WriteAsync("SupplierDeactivated", nameof(Supplier), s.Id, $"Name={s.Name}"); } catch { }
            return NoContent();
        }
    }
}
