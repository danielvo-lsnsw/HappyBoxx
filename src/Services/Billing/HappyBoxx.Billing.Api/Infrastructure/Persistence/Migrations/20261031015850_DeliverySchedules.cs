using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeliverySchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliverySchedules",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverySchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryScheduleItems",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UndeliveredReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryScheduleItems_DeliverySchedules_DeliveryScheduleId",
                        column: x => x.DeliveryScheduleId,
                        principalSchema: "billing",
                        principalTable: "DeliverySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryScheduleItems_DeliveryScheduleId_InvoiceId",
                schema: "billing",
                table: "DeliveryScheduleItems",
                columns: new[] { "DeliveryScheduleId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryScheduleItems_Status",
                schema: "billing",
                table: "DeliveryScheduleItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_CreatedAtUtc",
                schema: "billing",
                table: "DeliverySchedules",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySchedules_ScheduledDate",
                schema: "billing",
                table: "DeliverySchedules",
                column: "ScheduledDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryScheduleItems",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "DeliverySchedules",
                schema: "billing");
        }
    }
}
