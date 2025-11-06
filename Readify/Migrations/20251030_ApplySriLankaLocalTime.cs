using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class ApplySriLankaLocalTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add 5 hours 30 minutes to existing UTC timestamps to convert them to Sri Lanka local time
            migrationBuilder.Sql(@"
-- Users.CreatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Users' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [Users] SET [CreatedAt] = DATEADD(MINUTE, 330, [CreatedAt]);
END

-- Products.CreatedAt, Products.UpdatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [Product] SET [CreatedAt] = DATEADD(MINUTE, 330, [CreatedAt]);
END
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='UpdatedAt')
BEGIN
    UPDATE [Product] SET [UpdatedAt] = DATEADD(MINUTE, 330, [UpdatedAt]);
END

-- Orders.OrderDate
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderDate')
BEGIN
    UPDATE [Orders] SET [OrderDate] = DATEADD(MINUTE, 330, [OrderDate]);
END

-- EmailLogs.SentAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='EmailLogs' AND COLUMN_NAME='SentAt')
BEGIN
    UPDATE [EmailLogs] SET [SentAt] = DATEADD(MINUTE, 330, [SentAt]);
END

-- RefreshTokens.CreatedAt, RefreshTokens.ExpiresAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='RefreshTokens' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [RefreshTokens] SET [CreatedAt] = DATEADD(MINUTE, 330, [CreatedAt]);
END
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='RefreshTokens' AND COLUMN_NAME='ExpiresAt')
BEGIN
    UPDATE [RefreshTokens] SET [ExpiresAt] = DATEADD(MINUTE, 330, [ExpiresAt]);
END

-- PasswordResetTokens.ExpiresAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PasswordResetTokens' AND COLUMN_NAME='ExpiresAt')
BEGIN
    UPDATE [PasswordResetTokens] SET [ExpiresAt] = DATEADD(MINUTE, 330, [ExpiresAt]);
END

-- UserProfileUpdates.UpdatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserProfileUpdates' AND COLUMN_NAME='UpdatedAt')
BEGIN
    UPDATE [UserProfileUpdates] SET [UpdatedAt] = DATEADD(MINUTE, 330, [UpdatedAt]);
END

-- AuditLogs.Timestamp
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AuditLogs' AND COLUMN_NAME='Timestamp')
BEGIN
    UPDATE [AuditLogs] SET [Timestamp] = DATEADD(MINUTE, 330, [Timestamp]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: subtract 5 hours 30 minutes
            migrationBuilder.Sql(@"
-- Users.CreatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Users' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [Users] SET [CreatedAt] = DATEADD(MINUTE, -330, [CreatedAt]);
END

-- Products.CreatedAt, Products.UpdatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [Product] SET [CreatedAt] = DATEADD(MINUTE, -330, [CreatedAt]);
END
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='UpdatedAt')
BEGIN
    UPDATE [Product] SET [UpdatedAt] = DATEADD(MINUTE, -330, [UpdatedAt]);
END

-- Orders.OrderDate
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderDate')
BEGIN
    UPDATE [Orders] SET [OrderDate] = DATEADD(MINUTE, -330, [OrderDate]);
END

-- EmailLogs.SentAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='EmailLogs' AND COLUMN_NAME='SentAt')
BEGIN
    UPDATE [EmailLogs] SET [SentAt] = DATEADD(MINUTE, -330, [SentAt]);
END

-- RefreshTokens.CreatedAt, RefreshTokens.ExpiresAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='RefreshTokens' AND COLUMN_NAME='CreatedAt')
BEGIN
    UPDATE [RefreshTokens] SET [CreatedAt] = DATEADD(MINUTE, -330, [CreatedAt]);
END
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='RefreshTokens' AND COLUMN_NAME='ExpiresAt')
BEGIN
    UPDATE [RefreshTokens] SET [ExpiresAt] = DATEADD(MINUTE, -330, [ExpiresAt]);
END

-- PasswordResetTokens.ExpiresAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PasswordResetTokens' AND COLUMN_NAME='ExpiresAt')
BEGIN
    UPDATE [PasswordResetTokens] SET [ExpiresAt] = DATEADD(MINUTE, -330, [ExpiresAt]);
END

-- UserProfileUpdates.UpdatedAt
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='UserProfileUpdates' AND COLUMN_NAME='UpdatedAt')
BEGIN
    UPDATE [UserProfileUpdates] SET [UpdatedAt] = DATEADD(MINUTE, -330, [UpdatedAt]);
END

-- AuditLogs.Timestamp
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AuditLogs' AND COLUMN_NAME='Timestamp')
BEGIN
    UPDATE [AuditLogs] SET [Timestamp] = DATEADD(MINUTE, -330, [Timestamp]);
END
");
        }
    }
}
