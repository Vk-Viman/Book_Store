using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    /// <inheritdoc />
    public partial class AddCartAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter Title column to nvarchar(450) only if not already altered
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Product', N'U') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'Title' AND DATA_TYPE = 'nvarchar' AND (CHARACTER_MAXIMUM_LENGTH IS NULL OR CHARACTER_MAXIMUM_LENGTH <> 450))
    BEGIN
        DECLARE @dc sysname;
        SELECT @dc = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID(N'dbo.Product') AND c.name = N'Title';
        IF @dc IS NOT NULL
            EXEC(N'ALTER TABLE [dbo].[Product] DROP CONSTRAINT [' + @dc + ']');
        ALTER TABLE [dbo].[Product] ALTER COLUMN [Title] nvarchar(450) NOT NULL;
    END
END
");

            // Create AuditLogs if missing
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditLogs](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NULL,
        [Action] nvarchar(max) NOT NULL,
        [Entity] nvarchar(max) NOT NULL,
        [EntityId] int NULL,
        [Timestamp] datetime2 NOT NULL,
        [Details] nvarchar(max) NULL
    );
END
");

            // Create CartItems if missing
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CartItems](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL
    );
    IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CartItems_Product_ProductId')
    BEGIN
        ALTER TABLE [dbo].[CartItems] ADD CONSTRAINT FK_CartItems_Product_ProductId FOREIGN KEY (ProductId) REFERENCES [Product](Id) ON DELETE CASCADE;
    END
END
");

            // Create EmailLogs if missing
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[EmailLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailLogs](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [To] nvarchar(400) NOT NULL,
        [Subject] nvarchar(400) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [Success] bit NOT NULL,
        [Error] nvarchar(max) NULL,
        [Provider] nvarchar(100) NOT NULL
    );
END
");

            // Create Orders if missing (shipping columns will be added by later sync migration)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL
    );
END
");

            // Create UserProfileUpdates if missing
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[UserProfileUpdates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserProfileUpdates](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [OldFullName] nvarchar(max) NOT NULL,
        [OldEmail] nvarchar(max) NOT NULL,
        [NewFullName] nvarchar(max) NOT NULL,
        [NewEmail] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL
    );
END
");

            // Create OrderItems if missing
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL
    );
    IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders_OrderId')
        BEGIN
            ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT FK_OrderItems_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES [Orders](Id) ON DELETE CASCADE;
        END
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Product_ProductId')
        BEGIN
            ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT FK_OrderItems_Product_ProductId FOREIGN KEY (ProductId) REFERENCES [Product](Id) ON DELETE CASCADE;
        END
    END
END
");

            // Create indexes if missing (only create title index if column is suitable)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='Title' AND DATA_TYPE='nvarchar' AND CHARACTER_MAXIMUM_LENGTH >= 450)
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_Price' AND object_id = OBJECT_ID('Product'))
    BEGIN
        CREATE INDEX IX_Product_Price ON Product(Price);
    END
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_Title' AND object_id = OBJECT_ID('Product'))
    BEGIN
        CREATE INDEX IX_Product_Title ON Product(Title);
    END
END
ELSE
BEGIN
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_Price' AND object_id = OBJECT_ID('Product'))
    BEGIN
        CREATE INDEX IX_Product_Price ON Product(Price);
    END
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_CartItems_ProductId' AND object_id = OBJECT_ID('CartItems'))
BEGIN
    CREATE INDEX IX_CartItems_ProductId ON CartItems(ProductId);
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('OrderItems'))
BEGIN
    CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_ProductId' AND object_id = OBJECT_ID('OrderItems'))
BEGIN
    CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[OrderItems];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[UserProfileUpdates]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[UserProfileUpdates];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Orders];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[EmailLogs]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[EmailLogs];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[CartItems];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AuditLogs];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Product]', N'U') IS NOT NULL AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product' AND COLUMN_NAME='Title' AND DATA_TYPE='nvarchar' AND CHARACTER_MAXIMUM_LENGTH=450)
BEGIN
    ALTER TABLE [dbo].[Product] ALTER COLUMN [Title] nvarchar(max) NOT NULL;
END
");
        }
    }
}
