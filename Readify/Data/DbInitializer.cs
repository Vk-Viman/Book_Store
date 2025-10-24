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

        // Ensure PasswordResetTokens table exists in databases created without migrations
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
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure PasswordResetTokens table exists via raw SQL.");
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@readify.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Readify#Admin123!";

        if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var admin = new User
            {
                FullName = "Readify Admin",
                Email = adminEmail,
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default admin user {Email}", adminEmail);
        }

        // Run product/category seeder for demo content
        try
        {
            await DbSeeder.SeedAsync(context);
            logger.LogInformation("Seeded sample products and categories.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed products/categories.");
        }
    }
}
