using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddEnumsAndOrderTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns for orders table if not present
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
    END
END");

            // Ensure Users.Role column exists (it does), but nothing else to change here; Role enum is mapped to string property RoleString.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no-op
        }
    }
}
