using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddOrderHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[OrderHistories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderHistories](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] int NOT NULL,
        [OldStatus] nvarchar(max) NOT NULL,
        [NewStatus] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL
    );
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[OrderHistories]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[OrderHistories];
END");
        }
    }
}
