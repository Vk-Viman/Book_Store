using Readify.Models;

namespace Readify.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.Categories.Any()) return;

            var fiction = new Category { Name = "Fiction" };
            var nonFiction = new Category { Name = "Non-fiction" };
            var programming = new Category { Name = "Programming" };

            context.Categories.AddRange(fiction, nonFiction, programming);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Title = "Clean Code", Authors = "Robert C. Martin", ISBN = "9780132350884", Price = 29.99M, StockQty = 10, CategoryId = programming.Id, ImageUrl = "https://via.placeholder.com/150", Description = "A Handbook of Agile Software Craftsmanship" },
                new Product { Title = "The Pragmatic Programmer", Authors = "Andrew Hunt, David Thomas", ISBN = "9780201616224", Price = 24.99M, StockQty = 12, CategoryId = programming.Id, ImageUrl = "https://via.placeholder.com/150", Description = "Your Journey to Mastery" },
                new Product { Title = "1984", Authors = "George Orwell", ISBN = "9780451524935", Price = 9.99M, StockQty = 50, CategoryId = fiction.Id, ImageUrl = "https://via.placeholder.com/150", Description = "Dystopian novel" },
                new Product { Title = "Sapiens", Authors = "Yuval Noah Harari", ISBN = "9780062316097", Price = 19.99M, StockQty = 25, CategoryId = nonFiction.Id, ImageUrl = "https://via.placeholder.com/150", Description = "A Brief History of Humankind" }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}
