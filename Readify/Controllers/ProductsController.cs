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
        [HttpGet("{id:int}")]
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

        // GET /api/products/{id}/similar
        [HttpGet("{id:int}/similar")]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetSimilar(int id, [FromQuery] int take = 20)
        {
            var prod = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (prod == null) return NotFound(new { message = "Product not found" });
            take = Math.Clamp(take, 1, 50);

            // Same category (exclude itself)
            var sameCategory = _context.Products.Where(p => p.CategoryId == prod.CategoryId && p.Id != prod.Id);
            // Same authors (exact match; could be improved to token match)
            var sameAuthors = _context.Products.Where(p => p.Authors == prod.Authors && p.Id != prod.Id);
            // High rating (>=4) exclude itself
            var highRating = _context.Products.Where(p => (p.AvgRating ?? 0m) >= 4m && p.Id != prod.Id);

            // Union distinct by id
            var candidates = await sameCategory
                .Union(sameAuthors)
                .Union(highRating)
                .Distinct()
                .Take(200) // limit before scoring
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.ImageUrl,
                    p.Price,
                    p.AvgRating,
                    p.CategoryId,
                    p.Authors
                }).ToListAsync();

            // Simple scoring: +3 if same category, +2 if same authors, + (avgRating or 0)
            var scored = candidates.Select(c => new
            {
                c.Id,
                c.Title,
                c.ImageUrl,
                c.Price,
                c.AvgRating,
                Score = (c.CategoryId == prod.CategoryId ? 3 : 0) + (c.Authors == prod.Authors ? 2 : 0) + (double)(c.AvgRating ?? 0m)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Title)
            .Take(take)
            .ToList();

            return Ok(new { items = scored });
        }

        // GET /api/products/trending
        [HttpGet("trending")]
        [ResponseCache(Duration = 180, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetTrending([FromQuery] int take = 20)
        {
            take = Math.Clamp(take, 1, 50);

            // Sold quantities (OrderItems)
            var sold = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();
            var soldLookup = sold.ToDictionary(x => x.ProductId, x => x.Qty);

            // Wishlist counts
            var wish = await _context.Wishlists
                .GroupBy(w => w.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToListAsync();
            var wishLookup = wish.ToDictionary(x => x.ProductId, x => x.Count);

            // Load product baseline data for candidates (union of sold + wish + high rated)
            var candidateIds = soldLookup.Keys
                .Union(wishLookup.Keys)
                .Union(await _context.Products.Where(p => (p.AvgRating ?? 0m) >= 4m).Select(p => p.Id).ToListAsync())
                .ToList();
            if (!candidateIds.Any()) return Ok(new { items = Array.Empty<object>() });

            var products = await _context.Products.Where(p => candidateIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Title, p.ImageUrl, p.Price, p.AvgRating })
                .ToListAsync();

            var scored = products.Select(p => new
            {
                p.Id,
                p.Title,
                p.ImageUrl,
                p.Price,
                p.AvgRating,
                Score = (soldLookup.ContainsKey(p.Id) ? soldLookup[p.Id] * 2.0 : 0.0) + (wishLookup.ContainsKey(p.Id) ? wishLookup[p.Id] * 1.0 : 0.0) + (double)(p.AvgRating ?? 0m) * 3.0
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Title)
            .Take(take)
            .ToList();

            return Ok(new { items = scored });
        }
    }
}
