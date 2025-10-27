using Readify.DTOs;
using Readify.Models;

namespace Readify.Services
{
    public interface IMappingService
    {
        BookReadDto ToBookReadDto(Product p);
        Product FromBookCreateDto(BookCreateDto dto);
    }

    public class MappingService : IMappingService
    {
        public BookReadDto ToBookReadDto(Product p)
        {
            return new BookReadDto(p.Id, p.Title, p.Authors, p.Description, p.CategoryId, p.Category?.Name ?? string.Empty, p.Price, p.StockQty, p.ImageUrl);
        }

        public Product FromBookCreateDto(BookCreateDto dto)
        {
            return new Product
            {
                Title = dto.Title,
                Authors = dto.Authors,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                StockQty = dto.StockQty,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };
        }
    }
}
