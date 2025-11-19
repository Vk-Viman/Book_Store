// Archived FixPurchaseOrderItemFk migration
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class FixPurchaseOrderItemFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL
BEGIN
    -- Drop shadow column if it exists
    IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrderItems]') AND name = 'PurchaseOrderId1')
    BEGIN
        ALTER TABLE [dbo].[PurchaseOrderItems] DROP COLUMN [PurchaseOrderId1];
    END

    -- Ensure proper FK exists
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId')
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrderItems]') AND name = 'PurchaseOrderId')
        BEGIN
            ALTER TABLE [dbo].[PurchaseOrderItems] ADD CONSTRAINT FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId FOREIGN KEY (PurchaseOrderId) REFERENCES [PurchaseOrders](Id) ON DELETE CASCADE;
        END
    END

    -- Ensure index exists
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderItems_PurchaseOrderId' AND object_id = OBJECT_ID('PurchaseOrderItems'))
    BEGIN
        CREATE INDEX IX_PurchaseOrderItems_PurchaseOrderId ON PurchaseOrderItems(PurchaseOrderId);
    END
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- No destructive downgrade; recreate shadow column if missing (no data relationship)
IF OBJECT_ID(N'[dbo].[PurchaseOrderItems]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseOrderItems]') AND name = 'PurchaseOrderId1')
    BEGIN
        ALTER TABLE [dbo].[PurchaseOrderItems] ADD PurchaseOrderId1 int NULL;
    END
END
");
        }
    }
}
