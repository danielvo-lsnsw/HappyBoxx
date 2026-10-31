using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvoicePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PaidOn",
                schema: "billing",
                table: "Invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNote",
                schema: "billing",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                schema: "billing",
                table: "Invoices",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaidOn",
                schema: "billing",
                table: "Invoices",
                column: "PaidOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_PaidOn",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaidOn",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentNote",
                schema: "billing",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                schema: "billing",
                table: "Invoices");
        }
    }
}
