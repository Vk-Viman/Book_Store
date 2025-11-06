using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RowVersion safely
            migrationBuilder.Sql(@"
IF COL_LENGTH('Product','RowVersion') IS NULL
BEGIN
    ALTER TABLE [Product] ADD [RowVersion] rowversion;
END
");

            // Add Shipping columns to Orders only if they do not exist
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Orders','ShippingAddress') IS NULL
    BEGIN
        ALTER TABLE [Orders] ADD [ShippingAddress] nvarchar(max) NULL;
    END
    IF COL_LENGTH('Orders','ShippingName') IS NULL
    BEGIN
        ALTER TABLE [Orders] ADD [ShippingName] nvarchar(max) NULL;
    END
    IF COL_LENGTH('Orders','ShippingPhone') IS NULL
    BEGIN
        ALTER TABLE [Orders] ADD [ShippingPhone] nvarchar(max) NULL;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove columns if present
            migrationBuilder.Sql(@"
IF COL_LENGTH('Product','RowVersion') IS NOT NULL
BEGIN
    ALTER TABLE [Product] DROP COLUMN [RowVersion];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Orders','ShippingAddress') IS NOT NULL
    BEGIN
        ALTER TABLE [Orders] DROP COLUMN [ShippingAddress];
    END
    IF COL_LENGTH('Orders','ShippingName') IS NOT NULL
    BEGIN
        ALTER TABLE [Orders] DROP COLUMN [ShippingName];
    END
    IF COL_LENGTH('Orders','ShippingPhone') IS NOT NULL
    BEGIN
        ALTER TABLE [Orders] DROP COLUMN [ShippingPhone];
    END
END
");
        }
    }
}
