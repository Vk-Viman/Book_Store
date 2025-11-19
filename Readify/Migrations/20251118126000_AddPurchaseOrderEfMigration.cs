using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readify.Migrations
{
    public partial class AddPurchaseOrderEfMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns to PurchaseOrderItems
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedQuantity",
                table: "PurchaseOrderItems",
                nullable: false,
                defaultValue: 0);

            // Add columns to PurchaseOrders
            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "PurchaseOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            // Ensure index on PurchaseOrderItems.PurchaseOrderId exists
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            // Ensure foreign key relationship exists between PurchaseOrderItems and PurchaseOrders
            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseOrders");
        }
    }
}
