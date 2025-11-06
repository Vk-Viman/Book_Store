using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddPromoCodeAndCartMerge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [PaymentStatus] nvarchar(64) NOT NULL DEFAULT 'Pending';
    END
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderStatus')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [OrderStatus] nvarchar(64) NOT NULL DEFAULT 'Processing';
    END
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='PaymentTransactionId')
    BEGIN
        ALTER TABLE [dbo].[Orders] ADD [PaymentTransactionId] nvarchar(200) NULL;
    END
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoCodes");

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
