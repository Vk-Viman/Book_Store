IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251020095332_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251020095332_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251020095332_InitialCreate', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021051233_AddPasswordResetToken'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [Used] bit NOT NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021051233_AddPasswordResetToken'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021051233_AddPasswordResetToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251021051233_AddPasswordResetToken', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021055839_AddAuthExtras'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [Revoked] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021055839_AddAuthExtras'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021055839_AddAuthExtras'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251021055839_AddAuthExtras', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021081233_AddCatalogEntities'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ParentId] int NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021081233_AddCatalogEntities'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [ISBN] nvarchar(max) NOT NULL,
        [Authors] nvarchar(max) NOT NULL,
        [Publisher] nvarchar(max) NOT NULL,
        [ReleaseDate] datetime2 NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQty] int NOT NULL,
        [CategoryId] int NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [Language] nvarchar(max) NOT NULL,
        [Format] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021081233_AddCatalogEntities'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021081233_AddCatalogEntities'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021081233_AddCatalogEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251021081233_AddCatalogEntities', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_Categories_CategoryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    ALTER TABLE [Products] DROP CONSTRAINT [PK_Products];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    EXEC sp_rename N'[Products]', N'Product', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    EXEC sp_rename N'[Product].[IX_Products_CategoryId]', N'IX_Product_CategoryId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    ALTER TABLE [Product] ADD [CreatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    ALTER TABLE [Product] ADD CONSTRAINT [PK_Product] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    ALTER TABLE [Product] ADD CONSTRAINT [FK_Product_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027061204_AddPendingModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251027061204_AddPendingModelChanges', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_CartItems_ProductId' AND object_id = OBJECT_ID('CartItems'))
    BEGIN
        CREATE INDEX IX_CartItems_ProductId ON CartItems(ProductId);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('OrderItems'))
    BEGIN
        CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_ProductId' AND object_id = OBJECT_ID('OrderItems'))
    BEGIN
        CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251028083301_AddCartAndOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251028083301_AddCartAndOrders', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029044131_SyncModelChanges'
)
BEGIN

    IF COL_LENGTH('Product','RowVersion') IS NULL
    BEGIN
        ALTER TABLE [Product] ADD [RowVersion] rowversion;
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029044131_SyncModelChanges'
)
BEGIN

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

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029044131_SyncModelChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029044131_SyncModelChanges', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030074732_AddOrderPromoFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [DiscountAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030074732_AddOrderPromoFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [DiscountPercent] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030074732_AddOrderPromoFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [PromoCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030074732_AddOrderPromoFields'
)
BEGIN
    CREATE TABLE [PromoCodes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [DiscountPercent] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PromoCodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030074732_AddOrderPromoFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251030074732_AddOrderPromoFields', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030080949_AddShippingToOrderAndCheckoutDto'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [FixedAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030080949_AddShippingToOrderAndCheckoutDto'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [Type] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030080949_AddShippingToOrderAndCheckoutDto'
)
BEGIN
    ALTER TABLE [Orders] ADD [FreeShipping] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030080949_AddShippingToOrderAndCheckoutDto'
)
BEGIN
    ALTER TABLE [Orders] ADD [ShippingCost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251030080949_AddShippingToOrderAndCheckoutDto'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251030080949_AddShippingToOrderAndCheckoutDto', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106032255_AddOrderPaymentFields'
)
BEGIN
    IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL BEGIN
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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106032255_AddOrderPaymentFields'
)
BEGIN
    IF OBJECT_ID(N'[dbo].[ShippingSettings]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ShippingSettings](
            [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [Local] decimal(18,2) NOT NULL,
            [National] decimal(18,2) NOT NULL,
            [International] decimal(18,2) NOT NULL,
            [FreeShippingThreshold] decimal(18,2) NOT NULL,
            [UpdatedAt] datetime2 NOT NULL
        );
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106032255_AddOrderPaymentFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106032255_AddOrderPaymentFields', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106034413_AddIsActiveToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106034413_AddIsActiveToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106034413_AddIsActiveToUser', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112060547_ConvertRoleAndOrderStatus'
)
BEGIN
    IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='UpdatedAt')
        BEGIN
            ALTER TABLE [dbo].[Orders] ADD [UpdatedAt] datetime2 NULL;
        END
        IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='DateDelivered')
        BEGIN
            ALTER TABLE [dbo].[Orders] ADD [DateDelivered] datetime2 NULL;
        END
        IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Orders' AND COLUMN_NAME='OrderStatus')
        BEGIN
            ALTER TABLE [dbo].[Orders] ADD [OrderStatus] nvarchar(max) NULL;
            -- initialize existing rows to use legacy Status value where present
            UPDATE [dbo].[Orders] SET [OrderStatus] = [Status] WHERE [OrderStatus] IS NULL OR [OrderStatus] = '';
        END
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112060547_ConvertRoleAndOrderStatus'
)
BEGIN
    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        UPDATE [dbo].[Users] SET [Role] = CASE WHEN LTRIM(RTRIM([Role])) = '' THEN 'User' WHEN LOWER([Role]) LIKE 'admin%' THEN 'Admin' ELSE 'User' END WHERE [Role] IS NULL OR [Role] = '' OR LOWER([Role]) NOT IN ('admin','user');
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112060547_ConvertRoleAndOrderStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112060547_ConvertRoleAndOrderStatus', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    ALTER TABLE [Product] ADD [AvgRating] decimal(3,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE TABLE [OrderHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [OldStatus] nvarchar(max) NOT NULL,
        [NewStatus] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderHistories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsApproved] bit NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE TABLE [Wishlists] (
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([UserId], [ProductId]),
        CONSTRAINT [FK_Wishlists_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE INDEX [IX_Product_AvgRating] ON [Product] ([AvgRating]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE INDEX [IX_OrderHistories_OrderId] ON [OrderHistories] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    CREATE INDEX [IX_Wishlists_ProductId] ON [Wishlists] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113031103_DevSync'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113031103_DevSync', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113062757_AddInitialStockToProduct'
)
BEGIN
    -- Add InitialStock column if missing on either Product or Products table (idempotent)
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
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113062757_AddInitialStockToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113062757_AddInitialStockToProduct', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [ExpiryDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [GlobalUsageLimit] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [MinPurchase] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [PerUserLimit] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [PromoCodes] ADD [RemainingUses] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    ALTER TABLE [Orders] ADD [OriginalTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    CREATE TABLE [PromoCodeUsages] (
        [Id] int NOT NULL IDENTITY,
        [PromoCodeId] int NOT NULL,
        [UserId] int NOT NULL,
        [UsedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PromoCodeUsages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    CREATE INDEX [IX_PromoCodeUsages_PromoCodeId] ON [PromoCodeUsages] ([PromoCodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    CREATE INDEX [IX_PromoCodeUsages_PromoCodeId_UserId] ON [PromoCodeUsages] ([PromoCodeId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117100331_AddExtendedCouponFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251117100331_AddExtendedCouponFields', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118070039_RepairProductImages'
)
BEGIN
    IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NULL BEGIN
    CREATE TABLE [dbo].[ProductImages](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] INT NOT NULL,
        [ImageUrl] NVARCHAR(1024) NOT NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        CONSTRAINT [FK_ProductImages_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Product]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [dbo].[ProductImages]([ProductId],[SortOrder]);
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118070039_RepairProductImages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118070039_RepairProductImages', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119101447_EnsureSuppliersTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119101447_EnsureSuppliersTable', N'9.0.10');
END;

COMMIT;
GO

