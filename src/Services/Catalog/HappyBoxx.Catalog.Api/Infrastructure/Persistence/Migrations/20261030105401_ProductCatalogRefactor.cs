using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalogRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BuyPrice",
                schema: "catalog",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyCode",
                schema: "catalog",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackDetails",
                schema: "catalog",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PriceEffectiveDate",
                schema: "catalog",
                table: "Products",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellPriceFar",
                schema: "catalog",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellPriceNear",
                schema: "catalog",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "catalog",
                table: "Categories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE catalog.Products
                SET SellPriceNear = Price
                WHERE Price IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                WITH ExistingCategories AS (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAtUtc, Id) AS RowNo
                    FROM catalog.Categories
                )
                UPDATE c
                SET Code = CONCAT('CAT', RIGHT('000' + CAST(e.RowNo AS varchar(3)), 3))
                FROM catalog.Categories c
                INNER JOIN ExistingCategories e ON c.Id = e.Id
                WHERE c.Code IS NULL OR LTRIM(RTRIM(c.Code)) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "catalog",
                table: "Categories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Price",
                schema: "catalog",
                table: "Products");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSuppliers",
                schema: "catalog",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSuppliers", x => new { x.ProductId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "catalog",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Code",
                schema: "catalog",
                table: "Categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_ProductId_IsDefault",
                schema: "catalog",
                table: "ProductSuppliers",
                columns: new[] { "ProductId", "IsDefault" },
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_SupplierId",
                schema: "catalog",
                table: "ProductSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                schema: "catalog",
                table: "Suppliers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSuppliers",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Code",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "BuyPrice",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LegacyCode",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackDetails",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PriceEffectiveDate",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellPriceFar",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellPriceNear",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                schema: "catalog",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
