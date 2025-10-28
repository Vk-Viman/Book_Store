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
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure tables exist via raw SQL.");
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
