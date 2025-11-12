using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddWishlist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Wishlists]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Wishlists](
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT PK_Wishlists PRIMARY KEY (UserId, ProductId)
    );
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Wishlists]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Wishlists];
END");
        }
    }
}
