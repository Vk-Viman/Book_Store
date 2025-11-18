using Readify.Services;
using Readify.Models;
using Readify.DTOs;
using Xunit;

public class MappingServiceTests
{
    private readonly MappingService _mapper = new MappingService();

    [Fact]
    public void ToBookReadDto_MapsFieldsCorrectly()
    {
        var product = new Product
        {
            Id = 42,
            Title = "Test Title",
            Authors = "A. Author",
            Description = "Desc",
            CategoryId = 7,
            Category = new Readify.Models.Category { Id = 7, Name = "Cat" },
            Price = 9.99m,
            StockQty = 3,
            ImageUrl = "http://example.com/img.png",
            AvgRating = 4.25m
        };

        var dto = _mapper.ToBookReadDto(product);

        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Title, dto.Title);
        Assert.Equal(product.Authors, dto.Authors);
        Assert.Equal(product.Description, dto.Description);
        Assert.Equal(product.CategoryId, dto.CategoryId);
        Assert.Equal("Cat", dto.CategoryName);
        Assert.Equal(product.Price, dto.Price);
        Assert.Equal(product.StockQty, dto.StockQty);
        Assert.Equal(product.ImageUrl, dto.ImageUrl);
        Assert.Equal(product.AvgRating, dto.AvgRating);
    }

    [Fact]
    public void FromBookCreateDto_CreatesProductCorrectly()
    {
        var create = new BookCreateDto("New", "Author", "D", 2, 4.5m, 10, "http://img");

        var product = _mapper.FromBookCreateDto(create);

        Assert.Equal(create.Title, product.Title);
        Assert.Equal(create.Authors, product.Authors);
        Assert.Equal(create.Description, product.Description);
        Assert.Equal(create.CategoryId, product.CategoryId);
        Assert.Equal(create.Price, product.Price);
        Assert.Equal(create.StockQty, product.StockQty);
        Assert.Equal(create.ImageUrl, product.ImageUrl);
    }
}
