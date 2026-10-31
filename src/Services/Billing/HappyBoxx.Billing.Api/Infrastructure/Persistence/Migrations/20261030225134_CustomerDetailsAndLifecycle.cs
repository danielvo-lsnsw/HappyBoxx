using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerDetailsAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abn",
                schema: "billing",
                table: "Customers",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                schema: "billing",
                table: "Customers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "billing",
                table: "Customers",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                schema: "billing",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abn",
                schema: "billing",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                schema: "billing",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "billing",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Website",
                schema: "billing",
                table: "Customers");
        }
    }
}
