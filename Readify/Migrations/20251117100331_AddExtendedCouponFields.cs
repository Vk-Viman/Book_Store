using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedCouponFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "PromoCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GlobalUsageLimit",
                table: "PromoCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPurchase",
                table: "PromoCodes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerUserLimit",
                table: "PromoCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingUses",
                table: "PromoCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalTotal",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PromoCodeUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromoCodeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId_UserId",
                table: "PromoCodeUsages",
                columns: new[] { "PromoCodeId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoCodeUsages");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "GlobalUsageLimit",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "MinPurchase",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "PerUserLimit",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "RemainingUses",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "OriginalTotal",
                table: "Orders");
        }
    }
}
