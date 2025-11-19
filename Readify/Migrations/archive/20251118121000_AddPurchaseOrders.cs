// Archived original migration
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddPurchaseOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PurchaseOrders]', N'U') IS NULL
BEGIN
    CREATE TABLE [PurchaseOrders](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SupplierId] int NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ReceivedAt] datetime2 NULL,
        [ReceivedByUserId] int NULL
    );
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [PurchaseOrderItems](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PurchaseOrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL
    );
    IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId')
        BEGIN
            ALTER TABLE [PurchaseOrderItems] ADD CONSTRAINT FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId FOREIGN KEY (PurchaseOrderId) REFERENCES [PurchaseOrders](Id) ON DELETE CASCADE;
        END
    END
END
");
            migrationBuilder.Sql(@"IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderItems_PurchaseOrderId' AND object_id = OBJECT_ID('PurchaseOrderItems')) BEGIN CREATE INDEX IX_PurchaseOrderItems_PurchaseOrderId ON PurchaseOrderItems(PurchaseOrderId); END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL BEGIN DROP TABLE [PurchaseOrderItems]; END");
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[PurchaseOrders]', N'U') IS NOT NULL BEGIN DROP TABLE [PurchaseOrders]; END");
        }
    }
}
