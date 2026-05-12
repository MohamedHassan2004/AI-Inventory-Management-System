using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemBatchAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "StockBatches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "OrderItemBatchAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    StockBatchId = table.Column<int>(type: "int", nullable: false),
                    QuantityTaken = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemBatchAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemBatchAllocations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemBatchAllocations_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalTable: "StockBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatchAllocations_OrderItemId",
                table: "OrderItemBatchAllocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatchAllocations_StockBatchId",
                table: "OrderItemBatchAllocations",
                column: "StockBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemBatchAllocations");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "StockBatches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
