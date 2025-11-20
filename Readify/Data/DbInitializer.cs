using Microsoft.EntityFrameworkCore;
using Readify.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;

namespace Readify.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration config, ILogger logger, IWebHostEnvironment env)
    {
        // In Development use migrations to keep DB schema in sync with model changes.
        try
        {
            if (env.IsDevelopment())
            {
                logger.LogInformation("Development mode: applying migrations (Migrate)");
                await context.Database.MigrateAsync();
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
            // Also tolerate the case where EF detects pending model changes (common in active development) so the app can still start.
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
            else if (ex is InvalidOperationException ioe && (ioe.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase) || ioe.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase)))
            {
                // EF Core indicates that the model has pending changes which normally requires adding a migration.
                // In development environments we tolerate this and continue startup, but log instructions for the developer.
                logger.LogWarning(ioe, "EF Core model has pending changes. Skipping automatic migration. Consider adding a new migration and updating the database locally (dotnet ef migrations add <name> && dotnet ef database update).");
            }
            else
            {
                logger.LogError(ex, "Failed to apply migrations or ensure database created");
                throw;
            }
        }

        // Ensure Orders table has added columns (UpdatedAt, DateDelivered) if new fields were introduced in models
        try
        {
            var ensureOrdersColsSql = @"IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='UpdatedAt')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [UpdatedAt] datetime2 NULL;
    END
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='DateDelivered')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [DateDelivered] datetime2 NULL;
    END
END";
            await context.Database.ExecuteSqlRawAsync(ensureOrdersColsSql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure Orders table columns (UpdatedAt/DateDelivered). This is safe to ignore if DB schema already matches.");
        }

        // Ensure Products table has InitialStock column (add if missing)
        try
        {
            var ensureInitialStockSql = @"-- Add InitialStock column if missing on Product or Products table (idempotent)
IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='InitialStock')
    BEGIN
        ALTER TABLE [dbo].[Product] ADD [InitialStock] int NOT NULL DEFAULT 0;
    END
END

IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='InitialStock')
    BEGIN
        ALTER TABLE [dbo].[Products] ADD [InitialStock] int NOT NULL DEFAULT 0;
    END
END";
            await context.Database.ExecuteSqlRawAsync(ensureInitialStockSql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure Products table InitialStock column. This is safe to ignore if DB schema already matches.");
        }

        // NOTE: Suppliers and PurchaseOrders tables are created via EF migrations.
        // The application no longer creates these tables at runtime. If you see errors about missing tables,
        // run the EF migrations locally and on your target database:
        //   dotnet ef migrations add <name>
        //   dotnet ef database update
        // Or apply the generated SQL script `Readify/Migrations/phase14.sql` to your database.

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
                context.Users.Add(new User { FullName = "Demo Admin", Email = demoAdminEmail, Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword), IsActive = true });
            }

            // Ensure test/admin local user with known test password
            var localAdminEmail = "admin@readify.local";
            if (!await context.Users.AnyAsync(u => u.Email == localAdminEmail))
            {
                context.Users.Add(new User { FullName = "Local Admin", Email = localAdminEmail, Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(testAdminPassword), IsActive = true });
            }

            // Ensure demo user
            if (!await context.Users.AnyAsync(u => u.Email == demoUserEmail))
            {
                context.Users.Add(new User { FullName = "Demo User", Email = demoUserEmail, Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword), IsActive = true });
            }

            await context.SaveChangesAsync();

            // Seed sample products if none
            if (!await context.Products.AnyAsync())
            {
                var firstCategory = await context.Categories.OrderBy(c => c.Id).FirstAsync();
                var products = new[] {
                    new Product { Title = "The Pragmatic Programmer", Authors = "Andrew Hunt, David Thomas", Description = "Classic programming book.", Price = 25.99m, StockQty = 10, InitialStock = 10, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Product { Title = "Clean Code", Authors = "Robert C. Martin", Description = "A Handbook of Agile Software Craftsmanship.", Price = 29.99m, StockQty = 15, InitialStock = 15, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Product { Title = "Sapiens", Authors = "Yuval Noah Harari", Description = "A Brief History of Humankind.", Price = 19.99m, StockQty = 20, InitialStock = 20, CategoryId = firstCategory.Id, ImageUrl = "/images/book-placeholder.svg", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
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
