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
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] int[]? categoryIds, [FromQuery] string? author, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] decimal? minRating, [FromQuery] bool? inStock, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sort = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 100);

            // include Category so we can search by category name
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                // split query into terms (simple tokenization)
                var terms = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                if (terms.Any())
                {
                    foreach (var term in terms)
                    {
                        var t = term; // local copy for EF expression
                        query = query.Where(p => p.Title.Contains(t) || p.Description.Contains(t) || p.Authors.Contains(t) || p.ISBN.Contains(t) || (p.Category != null && p.Category.Name.Contains(t)));
                    }
                }
            }

            // category filters: support either single categoryId or multiple categoryIds
            if (categoryIds != null && categoryIds.Length > 0)
            {
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }
            else if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(p => p.Authors.Contains(author));
            }

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            if (minRating.HasValue)
            {
                query = query.Where(p => (p.AvgRating ?? 0m) >= minRating.Value);
            }

            if (inStock.HasValue && inStock.Value)
            {
                query = query.Where(p => p.StockQty > 0);
            }

            var total = await query.CountAsync();

            switch (sort)
            {
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "newest": query = query.OrderByDescending(p => p.CreatedAt); break;
                case "title_desc": query = query.OrderByDescending(p => p.Title); break;
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
                InitialStock = p.InitialStock,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                ImageUrl = p.ImageUrl,
                Language = p.Language,
                Format = p.Format,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                AvgRating = p.AvgRating
            }).ToList();

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return Ok(new { items = dtos, total, totalPages, page });
        }

        // GET /api/products/{id}
        [HttpGet("{id}")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
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
                InitialStock = product.InitialStock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                ImageUrl = product.ImageUrl,
                Language = product.Language,
                Format = product.Format,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                AvgRating = product.AvgRating
            };

            return Ok(dto);
        }
    }
}
