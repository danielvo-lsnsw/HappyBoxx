using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerMasterAndInvoiceProductLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "billing",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "billing",
                table: "Invoices",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceTier",
                schema: "billing",
                table: "InvoiceLines",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                schema: "billing",
                table: "InvoiceLines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "billing",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ProductId",
                schema: "billing",
                table: "InvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsActive",
                schema: "billing",
                table: "Customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Name",
                schema: "billing",
                table: "Customers",
                column: "Name",
                unique: true);

            migrationBuilder.InsertData(
                schema: "billing",
                table: "Customers",
                columns: new[] { "Id", "Name", "Address", "Phone", "IsActive", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000000"),
                    "Legacy customer (unmapped)",
                    "Unknown",
                    null,
                    false,
                    new DateTimeOffset(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)),
                    new DateTimeOffset(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc))
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                schema: "billing",
                table: "Invoices",
                column: "CustomerId",
                principalSchema: "billing",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DeleteData(
                schema: "billing",
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_ProductId",
                schema: "billing",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PriceTier",
                schema: "billing",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "billing",
                table: "InvoiceLines");
        }
    }
}
