using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Identity.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityAuditEvents",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetInvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuditEvents_CreatedAtUtc",
                schema: "identity",
                table: "IdentityAuditEvents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuditEvents_TargetAccountId",
                schema: "identity",
                table: "IdentityAuditEvents",
                column: "TargetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAuditEvents_TargetInvitationId",
                schema: "identity",
                table: "IdentityAuditEvents",
                column: "TargetInvitationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityAuditEvents",
                schema: "identity");
        }
    }
}
