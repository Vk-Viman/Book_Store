using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddSuppliers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NULL
BEGIN
    CREATE TABLE [Suppliers](
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL
    );
    CREATE INDEX IX_Suppliers_Name ON Suppliers(Name);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NOT NULL BEGIN DROP TABLE [Suppliers]; END");
        }
    }
}
