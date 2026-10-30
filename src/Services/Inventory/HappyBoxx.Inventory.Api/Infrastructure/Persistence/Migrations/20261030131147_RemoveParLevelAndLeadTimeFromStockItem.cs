using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveParLevelAndLeadTimeFromStockItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "ParLevel",
                schema: "inventory",
                table: "StockItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                schema: "inventory",
                table: "StockItems",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<decimal>(
                name: "ParLevel",
                schema: "inventory",
                table: "StockItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
