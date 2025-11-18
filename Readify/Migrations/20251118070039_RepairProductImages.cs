using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    /// <inheritdoc />
    public partial class RepairProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NULL BEGIN
CREATE TABLE [dbo].[ProductImages](
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ProductId] INT NOT NULL,
    [ImageUrl] NVARCHAR(1024) NOT NULL,
    [SortOrder] INT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_ProductImages_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Product]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [dbo].[ProductImages]([ProductId],[SortOrder]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NOT NULL DROP TABLE [dbo].[ProductImages];");
        }
    }
}
