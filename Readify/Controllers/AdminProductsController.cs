using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Readify.Data;
using Readify.Models;
using Readify.Services;

namespace Readify.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminProductsController> _logger;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;

        public AdminProductsController(AppDbContext context, ILogger<AdminProductsController> logger, IAuditService audit, IEmailService email, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _audit = audit;
            _email = email;
            _config = config;
        }

        // POST api/admin/products
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(product.Title))
                return BadRequest(new { message = "Title is required." });

            // Validate category exists
            var category = await _context.Categories.FindAsync(product.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Selected category does not exist. Please select or create a category." });
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Add(product);

            try
            {
                await _context.SaveChangesAsync();
                await _audit.WriteAsync("Create", nameof(Product), product.Id);

                var adminEmail = _config["Notifications:AdminEmail"] ?? _config["Seed:AdminEmail"]; // fallback
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    _ = _email.SendTemplateAsync(adminEmail, "AdminProductCreated", new { product.Title, product.Id, product.Price, product.StockQty });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update failed while creating product.");
                // For debugging return error details in development
                return StatusCode(500, new { message = "Failed to save product to the database.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating product.");
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // PUT api/admin/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product updated)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Validate category
            var category = await _context.Categories.FindAsync(updated.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = "Selected category does not exist. Please select or create a category." });
            }

            product.Title = updated.Title;
            product.Description = updated.Description;
            product.ISBN = updated.ISBN;
            product.Authors = updated.Authors;
            product.Publisher = updated.Publisher;
            product.ReleaseDate = updated.ReleaseDate;
            product.Price = updated.Price;
            product.StockQty = updated.StockQty;
            product.CategoryId = updated.CategoryId;
            product.ImageUrl = updated.ImageUrl;
            product.Language = updated.Language;
            product.Format = updated.Format;
            product.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                await _audit.WriteAsync("Update", nameof(Product), product.Id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update failed while updating product {Id}.", id);
                return StatusCode(500, new { message = "Failed to update product in the database.", detail = dbEx.Message });
            }

            return NoContent();
        }

        // DELETE api/admin/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            await _audit.WriteAsync("Delete", nameof(Product), id);
            return NoContent();
        }

        // GET api/admin/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }
    }
}
