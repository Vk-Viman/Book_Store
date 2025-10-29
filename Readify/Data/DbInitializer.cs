using Microsoft.EntityFrameworkCore;
using Readify.Models;
using Microsoft.AspNetCore.Hosting;

namespace Readify.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration config, ILogger logger, IWebHostEnvironment env)
    {
        // Apply migrations to bring database schema up-to-date.
        // For production this is the recommended approach instead of executing ad-hoc SQL.
        try
        {
            logger.LogInformation("Applying pending migrations (if any)");
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            // Migration failures are serious; log and rethrow to fail fast in CI/production.
            logger.LogError(ex, "Failed to apply migrations");
            throw;
        }

        // Seed demo users and sample data for local/offline mode
        try
        {
            // Seed categories if none
            if (!await context.Categories.AnyAsync())
            {
                var categories = new[] {
                    new Category { Name = "Fiction" },
                    new Category { Name = "Non-fiction" },
                    new Category { Name = "Programming" },
                    new Category { Name = "Self-Help / Personal Development" }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded default categories.");
            }

            // Seed demo users
            var demoAdminEmail = config["Seed:AdminEmail"] ?? "admin@demo.com";
            var demoUserEmail = config["Seed:UserEmail"] ?? "user@demo.com";
            var demoPassword = config["Seed:AdminPassword"] ?? "Readify#Demo123!";

            if (!await context.Users.AnyAsync(u => u.Email == demoAdminEmail))
            {
                context.Users.Add(new User { FullName = "Demo Admin", Email = demoAdminEmail, Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword) });
            }
            if (!await context.Users.AnyAsync(u => u.Email == demoUserEmail))
            {
                context.Users.Add(new User { FullName = "Demo User", Email = demoUserEmail, Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword) });
            }

            await context.SaveChangesAsync();

            // Seed sample products if none
            if (!await context.Products.AnyAsync())
            {
                var firstCategory = await context.Categories.OrderBy(c => c.Id).FirstAsync();
                var products = new[] {
                    new Product { Title = "The Pragmatic Programmer", Authors = "Andrew Hunt, David Thomas", Description = "Classic programming book.", Price = 25.99m, StockQty = 10, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Product { Title = "Clean Code", Authors = "Robert C. Martin", Description = "A Handbook of Agile Software Craftsmanship.", Price = 29.99m, StockQty = 15, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Product { Title = "Sapiens", Authors = "Yuval Noah Harari", Description = "A Brief History of Humankind.", Price = 19.99m, StockQty = 20, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded sample products.");
            }

            // Seed cart items for demo user (optional) - skip in Development to start empty
            try
            {
                if (!env.IsDevelopment())
                {
                    var demoUser = await context.Users.FirstOrDefaultAsync(u => u.Email == (config["Seed:UserEmail"] ?? "user@demo.com"));
                    if (demoUser != null && !await context.CartItems.AnyAsync(c => c.UserId == demoUser.Id))
                    {
                        var sampleProduct = await context.Products.OrderBy(p => p.Id).FirstOrDefaultAsync();
                        if (sampleProduct != null)
                        {
                            context.CartItems.Add(new CartItem { UserId = demoUser.Id, ProductId = sampleProduct.Id, Quantity = 1 });
                            await context.SaveChangesAsync();
                            logger.LogInformation("Seeded sample cart item for demo user.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed demo cart items.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed demo data.");
        }
    }
}
