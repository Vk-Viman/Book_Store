using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddInitialStockToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"-- Add InitialStock column if missing on either Product or Products table (idempotent)
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
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"-- Remove InitialStock column if present on Product or Products table (idempotent)
IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='InitialStock')
    BEGIN
        ALTER TABLE [dbo].[Product] DROP COLUMN [InitialStock];
    END
END

IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='InitialStock')
    BEGIN
        ALTER TABLE [dbo].[Products] DROP COLUMN [InitialStock];
    END
END");
        }
    }
}
