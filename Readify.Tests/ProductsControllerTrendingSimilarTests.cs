using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readify.Controllers;
using Readify.Data;
using Readify.Models;
using Xunit;

public class ProductsControllerTrendingSimilarTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("prod_trending_similar_tests").Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Trending_Returns_Ordered_By_Score()
    {
        await using var db = CreateDb();
        // products
        var p1 = new Product { Id = 1, Title = "A", Price = 10m };
        var p2 = new Product { Id = 2, Title = "B", Price = 12m };
        var p3 = new Product { Id = 3, Title = "C", Price = 15m };
        db.Products.AddRange(p1, p2, p3);
        // order items (make p2 best-seller)
        db.Orders.Add(new Order { Id = 1, UserId = 1, TotalAmount = 24m });
        db.OrderItems.Add(new OrderItem { OrderId = 1, ProductId = 2, Quantity = 3, UnitPrice = 4m });
        db.OrderItems.Add(new OrderItem { OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 10m });
        // wishlists to add weight to p3
        db.Wishlists.Add(new Wishlist { UserId = 2, ProductId = 3 });
        db.Wishlists.Add(new Wishlist { UserId = 3, ProductId = 3 });
        // ratings
        p1.AvgRating = 3.5m;
        p2.AvgRating = 4.5m;
        p3.AvgRating = 4.0m;
        await db.SaveChangesAsync();

        var controller = new ProductsController(db);
        var result = await controller.GetTrending(20) as OkObjectResult;
        Assert.NotNull(result);
        dynamic payload = result.Value!;
        var items = ((System.Collections.IEnumerable)payload.items).Cast<dynamic>().ToList();
        Assert.True(items.Count >= 3);
        // first item should be product 2 given highest composite score
        Assert.Equal(2, (int)items[0].Id);
    }

    [Fact]
    public async Task Similar_Returns_Same_Category_And_Author_Boosted()
    {
        await using var db = CreateDb();
        var cat1 = 1; var cat2 = 2;
        var prod = new Product { Id = 10, Title = "Ref", CategoryId = cat1, Authors = "Author X", Price = 10m };
        var sameCat = new Product { Id = 11, Title = "SameCat", CategoryId = cat1, Authors = "Other", Price = 9m, AvgRating = 4m };
        var sameAuthor = new Product { Id = 12, Title = "SameAuthor", CategoryId = cat2, Authors = "Author X", Price = 11m, AvgRating = 4.5m };
        var highRated = new Product { Id = 13, Title = "HighRated", CategoryId = cat2, Authors = "Y", Price = 15m, AvgRating = 4.8m };
        db.Products.AddRange(prod, sameCat, sameAuthor, highRated);
        await db.SaveChangesAsync();

        var controller = new ProductsController(db);
        var result = await controller.GetSimilar(10, 10) as OkObjectResult;
        Assert.NotNull(result);
        dynamic payload = result.Value!;
        var items = ((System.Collections.IEnumerable)payload.items).Cast<dynamic>().ToList();
        Assert.True(items.Any());
        var ids = items.Select(i => (int)i.Id).ToList();
        Assert.Contains(11, ids); // same category
        Assert.Contains(12, ids); // same author
        // ensure reference id excluded
        Assert.DoesNotContain(10, ids);
    }
}
