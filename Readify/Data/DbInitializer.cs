using Microsoft.EntityFrameworkCore;
using Readify.Models;

namespace Readify.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration config, ILogger logger)
    {
        // Apply pending migrations (recommended) otherwise ensure database created
        try
        {
            var pending = context.Database.GetPendingMigrations();
            if (pending != null && pending.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations.", pending.Count());
                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("No migrations found; ensuring database is created from model.");
                await context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to apply migrations automatically, falling back to EnsureCreated().");
            await context.Database.EnsureCreatedAsync();
        }

        // Ensure additional tables exist when migrations are not kept in sync
        try
        {
            var createPasswordResetSql = @"
IF OBJECT_ID(N'[dbo].[PasswordResetTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [Token] NVARCHAR(4000) NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [Used] BIT NOT NULL DEFAULT 0
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createPasswordResetSql);
            logger.LogInformation("Ensured PasswordResetTokens table exists.");

            var createAuditSql = @"
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditLogs](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NULL,
        [Action] NVARCHAR(200) NOT NULL,
        [Entity] NVARCHAR(200) NOT NULL,
        [EntityId] INT NULL,
        [Timestamp] DATETIME2 NOT NULL,
        [Details] NVARCHAR(MAX) NULL
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createAuditSql);

            var createEmailLogSql = @"
IF OBJECT_ID(N'[dbo].[EmailLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailLogs](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [To] NVARCHAR(400) NOT NULL,
        [Subject] NVARCHAR(400) NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [SentAt] DATETIME2 NOT NULL,
        [Success] BIT NOT NULL,
        [Error] NVARCHAR(MAX) NULL,
        [Provider] NVARCHAR(100) NOT NULL
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createEmailLogSql);

            var createUserProfileSql = @"
IF OBJECT_ID(N'[dbo].[UserProfileUpdates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserProfileUpdates](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [OldFullName] NVARCHAR(400) NOT NULL,
        [OldEmail] NVARCHAR(400) NOT NULL,
        [NewFullName] NVARCHAR(400) NOT NULL,
        [NewEmail] NVARCHAR(400) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createUserProfileSql);

            // Ensure cart and order related tables exist
            var createCartSql = @"
IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CartItems](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES [Product](Id)
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createCartSql);

            var createOrdersSql = @"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [OrderDate] DATETIME2 NOT NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [Status] NVARCHAR(100) NOT NULL
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createOrdersSql);

            var createOrderItemsSql = @"
IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES [Orders](Id),
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES [Product](Id)
    );
END
";
            await context.Database.ExecuteSqlRawAsync(createOrderItemsSql);

            logger.LogInformation("Ensured CartItems, Orders and OrderItems tables exist.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure tables exist via raw SQL.");
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

            // Seed cart items for demo user (optional)
            try
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
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed demo cart items.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed demo data.");
        }

        // Keep running original seeder for backward compatibility
        try
        {
            await DbSeeder.SeedAsync(context);
            logger.LogInformation("Ran legacy DbSeeder.");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Legacy DbSeeder failed or not present.");
        }
    }
}
