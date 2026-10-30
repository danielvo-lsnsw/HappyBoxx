using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReorderPolicyAndDemandTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReorderLevel",
                schema: "inventory",
                table: "StockItems",
                newName: "ParLevel");

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                schema: "inventory",
                table: "StockItems",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLevel",
                schema: "inventory",
                table: "StockItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinLevel",
                schema: "inventory",
                table: "StockItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ProductId_OccurredAtUtc",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "ProductId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustments",
                schema: "inventory");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "MaxLevel",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "MinLevel",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.RenameColumn(
                name: "ParLevel",
                schema: "inventory",
                table: "StockItems",
                newName: "ReorderLevel");
        }
    }
}
