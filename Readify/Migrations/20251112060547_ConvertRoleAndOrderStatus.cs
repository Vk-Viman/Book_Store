using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class ConvertRoleAndOrderStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add order lifecycle columns only if missing
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
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
END");

            // Ensure Users.Role values are normalized to 'User' or 'Admin'
            // (existing Role column remains string, but we ensure known values)
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Users] SET [Role] = CASE WHEN LTRIM(RTRIM([Role])) = '' THEN 'User' WHEN LOWER([Role]) LIKE 'admin%' THEN 'Admin' ELSE 'User' END WHERE [Role] IS NULL OR [Role] = '' OR LOWER([Role]) NOT IN ('admin','user');
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no-op: schema changes are additive and should be preserved
        }
    }
}
