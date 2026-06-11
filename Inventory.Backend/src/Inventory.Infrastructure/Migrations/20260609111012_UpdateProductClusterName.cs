using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductClusterName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ProductClusters");

            migrationBuilder.AddColumn<string>(
                name: "ClusterName",
                table: "ProductClusters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterName",
                table: "ProductClusters");

            migrationBuilder.AddColumn<int>(
                name: "ClusterId",
                table: "ProductClusters",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
