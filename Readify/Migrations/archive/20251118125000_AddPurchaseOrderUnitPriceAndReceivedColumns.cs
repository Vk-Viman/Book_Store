// Archived SQL migration
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddPurchaseOrderUnitPriceAndReceivedColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('PurchaseOrderItems', 'UnitPrice') IS NULL
        ALTER TABLE PurchaseOrderItems ADD UnitPrice decimal(18,2) NOT NULL DEFAULT(0);
    IF COL_LENGTH('PurchaseOrderItems', 'ReceivedQuantity') IS NULL
        ALTER TABLE PurchaseOrderItems ADD ReceivedQuantity int NOT NULL DEFAULT(0);
END

IF OBJECT_ID(N'[dbo].[PurchaseOrders]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('PurchaseOrders', 'TotalAmount') IS NULL
        ALTER TABLE PurchaseOrders ADD TotalAmount decimal(18,2) NOT NULL DEFAULT(0);
    IF COL_LENGTH('PurchaseOrders', 'CreatedAt') IS NULL
        ALTER TABLE PurchaseOrders ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
    IF COL_LENGTH('PurchaseOrders', 'UpdatedAt') IS NULL
        ALTER TABLE PurchaseOrders ADD UpdatedAt datetime2 NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('PurchaseOrderItems', 'UnitPrice') IS NOT NULL
        ALTER TABLE PurchaseOrderItems DROP COLUMN UnitPrice;
    IF COL_LENGTH('PurchaseOrderItems', 'ReceivedQuantity') IS NOT NULL
        ALTER TABLE PurchaseOrderItems DROP COLUMN ReceivedQuantity;
END

IF OBJECT_ID(N'[dbo].[PurchaseOrders]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('PurchaseOrders', 'TotalAmount') IS NOT NULL
        ALTER TABLE PurchaseOrders DROP COLUMN TotalAmount;
    IF COL_LENGTH('PurchaseOrders', 'CreatedAt') IS NOT NULL
        ALTER TABLE PurchaseOrders DROP COLUMN CreatedAt;
    IF COL_LENGTH('PurchaseOrders', 'UpdatedAt') IS NOT NULL
        ALTER TABLE PurchaseOrders DROP COLUMN UpdatedAt;
END
");
        }
    }
}
