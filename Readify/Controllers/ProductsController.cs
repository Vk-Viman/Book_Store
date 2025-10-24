using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.DTOs;

namespace Readify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/products
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] string? author, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sort = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Title.Contains(q) || p.Description.Contains(q) || p.Authors.Contains(q) || p.ISBN.Contains(q));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(p => p.Authors.Contains(author));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            switch (sort)
            {
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "newest": query = query.OrderByDescending(p => p.CreatedAt); break;
                default: query = query.OrderBy(p => p.Title); break;
            }

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.Category)
                .ToListAsync();

            var dtos = items.Select(p => new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                ISBN = p.ISBN,
                Authors = p.Authors,
                Publisher = p.Publisher,
                ReleaseDate = p.ReleaseDate,
                Price = p.Price,
                StockQty = p.StockQty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                ImageUrl = p.ImageUrl,
                Language = p.Language,
                Format = p.Format,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(new { items = dtos, total, totalPages, page });
        }

        // GET /api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                ISBN = product.ISBN,
                Authors = product.Authors,
                Publisher = product.Publisher,
                ReleaseDate = product.ReleaseDate,
                Price = product.Price,
                StockQty = product.StockQty,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                ImageUrl = product.ImageUrl,
                Language = product.Language,
                Format = product.Format,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(dto);
        }
    }
}
