using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.DTOs;
using Readify.Models;

namespace Readify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/books
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] string? author, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? sort, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 100);

            var query = _context.Products.AsQueryable(); // reuse Products as Books

            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Title.Contains(q) || p.Description.Contains(q) || p.Authors.Contains(q));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(author)) query = query.Where(p => p.Authors.Contains(author));
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            // Sorting: support both (sort=value_dir) and (sort=value + sortDir)
            var s = (sort ?? string.Empty).ToLowerInvariant();
            var d = (sortDir ?? string.Empty).ToLowerInvariant();

            query = s switch
            {
                // one-parameter mode used by frontend
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "title_desc" => query.OrderByDescending(p => p.Title),
                "title_asc" => query.OrderBy(p => p.Title),

                // two-parameter legacy mode
                "title" when d == "desc" => query.OrderByDescending(p => p.Title),
                "title" => query.OrderBy(p => p.Title),
                "price" when d == "desc" => query.OrderByDescending(p => p.Price),
                "price" => query.OrderBy(p => p.Price),
                "createdat" when d == "desc" => query.OrderByDescending(p => p.CreatedAt),
                "createdat" => query.OrderBy(p => p.CreatedAt),

                // default: Title ascending
                _ => query.OrderBy(p => p.Title)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Include(p => p.Category).ToListAsync();

            var dtos = items.Select(p => new BookReadDto(p.Id, p.Title, p.Authors, p.Description, p.CategoryId, p.Category?.Name ?? string.Empty, p.Price, p.StockQty, p.ImageUrl)).ToList();

            return Ok(new { items = dtos, total, totalPages = (int)Math.Ceiling(total / (double)pageSize), page });
        }

        // GET /api/books/{id}
        [HttpGet("{id}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();
            var dto = new BookReadDto(p.Id, p.Title, p.Authors, p.Description, p.CategoryId, p.Category?.Name ?? string.Empty, p.Price, p.StockQty, p.ImageUrl);
            return Ok(dto);
        }

        // POST /api/books
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(BookCreateDto input)
        {
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            var product = new Readify.Models.Product
            {
                Title = input.Title,
                Authors = input.Authors,
                Description = input.Description,
                CategoryId = input.CategoryId,
                Price = input.Price,
                StockQty = input.StockQty,
                ImageUrl = input.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            var dto = new BookReadDto(product.Id, product.Title, product.Authors, product.Description, product.CategoryId, string.Empty, product.Price, product.StockQty, product.ImageUrl);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, dto);
        }

        // PUT /api/books/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, BookCreateDto input)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var isAdmin = User.IsInRole("Admin");
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int userId = string.IsNullOrEmpty(userIdClaim) ? 0 : int.Parse(userIdClaim);

            if (!isAdmin && product.CreatedByUserId != userId)
            {
                return Forbid();
            }

            product.Title = input.Title;
            product.Authors = input.Authors;
            product.Description = input.Description;
            product.CategoryId = input.CategoryId;
            product.Price = input.Price;
            product.StockQty = input.StockQty;
            product.ImageUrl = input.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/books/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
