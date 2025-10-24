using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Data;
using Readify.Models;
using Readify.DTOs;

namespace Readify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cats = await _context.Categories.ToListAsync();
            var dtos = cats.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, ParentId = c.ParentId }).ToList();
            return Ok(dtos);
        }
    }
}
