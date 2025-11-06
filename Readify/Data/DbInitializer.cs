using Microsoft.EntityFrameworkCore;
using Readify.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;

namespace Readify.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration config, ILogger logger, IWebHostEnvironment env)
    {
        // In Development use EnsureCreated to avoid migration conflicts from ad-hoc changes.
        try
        {
            if (env.IsDevelopment())
            {
                logger.LogInformation("Development mode: ensuring database is created (EnsureCreated)");
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                logger.LogInformation("Applying pending migrations (if any)");
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            // If migration fails due to pre-existing objects created outside of migrations, log and continue.
            if (ex is SqlException sqlEx)
            {
                if (sqlEx.Number == 2714 || sqlEx.Number == 2705 || sqlEx.Number == 2719)
                {
                    logger.LogWarning(sqlEx, "Migrations encountered existing objects (likely from prior ad-hoc initialization). Continuing startup.");
                }
                else
                {
                    logger.LogError(ex, "Failed to apply migrations or ensure database created");
                    throw;
                }
            }
            else
            {
                logger.LogError(ex, "Failed to apply migrations or ensure database created");
                throw;
            }
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
            // Support multiple admin emails used by tests and demo
            var demoPassword = config["Seed:AdminPassword"] ?? "Readify#Demo123!";
            var testAdminPassword = "Readify#Admin123!"; // used by integration tests & Cypress

            var demoAdminEmail = config["Seed:AdminEmail"] ?? "admin@demo.com";
            var demoUserEmail = config["Seed:UserEmail"] ?? "user@demo.com";

            // Ensure demo admin
            if (!await context.Users.AnyAsync(u => u.Email == demoAdminEmail))
            {
                context.Users.Add(new User { FullName = "Demo Admin", Email = demoAdminEmail, Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword) });
            }

            // Ensure test/admin local user with known test password
            var localAdminEmail = "admin@readify.local";
            if (!await context.Users.AnyAsync(u => u.Email == localAdminEmail))
            {
                context.Users.Add(new User { FullName = "Local Admin", Email = localAdminEmail, Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(testAdminPassword) });
            }

            // Ensure demo user
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

            // Seed default shipping settings if none
            try
            {
                // Attempt to query ShippingSettings; if the table is missing, create it via SQL then insert default
                var exists = false;
                try
                {
                    exists = await context.ShippingSettings.AnyAsync();
                }
                catch (SqlException sex) when (sex.Number == 208) // Invalid object name
                {
                    logger.LogWarning(sex, "ShippingSettings table not found. Creating it as part of initialization.");
                    var createSql = @"
IF OBJECT_ID(N'[dbo].[ShippingSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ShippingSettings](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Local] decimal(18,2) NOT NULL,
        [National] decimal(18,2) NOT NULL,
        [International] decimal(18,2) NOT NULL,
        [FreeShippingThreshold] decimal(18,2) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL
    );
END
";
                    await context.Database.ExecuteSqlRawAsync(createSql);
                    exists = false;
                }

                if (!exists)
                {
                    context.ShippingSettings.Add(new ShippingSetting { Local = 2m, National = 5m, International = 15m, FreeShippingThreshold = 100m, UpdatedAt = DateTime.UtcNow });
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded default shipping settings.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed shipping settings.");
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
