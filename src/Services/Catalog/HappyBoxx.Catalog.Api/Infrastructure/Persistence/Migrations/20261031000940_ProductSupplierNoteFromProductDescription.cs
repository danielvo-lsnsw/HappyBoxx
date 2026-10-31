using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Catalog.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductSupplierNoteFromProductDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "catalog",
                table: "ProductSuppliers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ps
                SET ps.Note = p.Description
                FROM [catalog].[ProductSuppliers] AS ps
                INNER JOIN [catalog].[Products] AS p ON p.Id = ps.ProductId
                WHERE p.Description IS NOT NULL
                  AND LEN(LTRIM(RTRIM(p.Description))) > 0
                  AND (ps.Note IS NULL OR LEN(LTRIM(RTRIM(ps.Note))) = 0);
                """);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.Description = NULL
                FROM [catalog].[Products] AS p
                WHERE p.Description IS NOT NULL
                  AND LEN(LTRIM(RTRIM(p.Description))) > 0
                  AND EXISTS (
                      SELECT 1
                      FROM [catalog].[ProductSuppliers] AS ps
                      WHERE ps.ProductId = p.Id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                schema: "catalog",
                table: "ProductSuppliers");
        }
    }
}
