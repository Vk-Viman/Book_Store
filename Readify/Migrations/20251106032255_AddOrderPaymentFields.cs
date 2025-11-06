using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to Orders only if they don't already exist
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [OrderStatus] nvarchar(max) NOT NULL DEFAULT '';
    END
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [PaymentStatus] nvarchar(max) NOT NULL DEFAULT '';
    END
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentTransactionId')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [PaymentTransactionId] nvarchar(max) NULL;
    END
END");

            // Create ShippingSettings only if missing
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[ShippingSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [ShippingSettings](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Local] decimal(18,2) NOT NULL,
        [National] decimal(18,2) NOT NULL,
        [International] decimal(18,2) NOT NULL,
        [FreeShippingThreshold] decimal(18,2) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL
    );
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop ShippingSettings only if exists
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[ShippingSettings]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ShippingSettings];
END");

            // Drop columns from Orders if they exist
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL BEGIN
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentTransactionId')
    BEGIN
        ALTER TABLE [dbo].[Orders] DROP COLUMN [PaymentTransactionId];
    END
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] DROP COLUMN [OrderStatus];
    END
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] DROP COLUMN [PaymentStatus];
    END
END");
        }
    }
}
