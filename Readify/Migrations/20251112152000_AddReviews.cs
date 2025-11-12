using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddReviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Reviews]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Reviews](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] int NOT NULL,
        [UserId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsApproved] bit NOT NULL
    );
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Reviews]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Reviews];
END");
        }
    }
}
