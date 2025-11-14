using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddAvgRatingToProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='AvgRating')
    BEGIN
        ALTER TABLE [dbo].[Products] ADD [AvgRating] decimal(3,2) NULL;
    END
END");

            // backfill avg from approved reviews
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Reviews]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
BEGIN
    UPDATE p SET AvgRating = r.avg
    FROM [dbo].[Products] p
    CROSS APPLY (
        SELECT AVG(CAST(Rating AS decimal(3,2))) avg FROM [dbo].[Reviews] WHERE ProductId = p.Id AND IsApproved = 1
    ) r
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='AvgRating')
    BEGIN
        ALTER TABLE [dbo].[Products] DROP COLUMN [AvgRating];
    END
END");
        }
    }
}
