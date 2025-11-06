using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddRowVersionToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RowVersion column if it does not already exist - idempotent for environments
            migrationBuilder.Sql(@"
IF COL_LENGTH('Product','RowVersion') IS NULL
BEGIN
    ALTER TABLE [Product] ADD [RowVersion] rowversion;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Product','RowVersion') IS NOT NULL
BEGIN
    ALTER TABLE [Product] DROP COLUMN [RowVersion];
END
");
        }
    }
}
