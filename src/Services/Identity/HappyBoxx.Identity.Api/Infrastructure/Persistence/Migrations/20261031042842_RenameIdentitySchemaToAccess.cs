using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Identity.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameIdentitySchemaToAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_IdentityAuditEvents",
                schema: "identity",
                table: "IdentityAuditEvents");

            migrationBuilder.EnsureSchema(
                name: "access");

            migrationBuilder.RenameTable(
                name: "StaffInvitations",
                schema: "identity",
                newName: "StaffInvitations",
                newSchema: "access");

            migrationBuilder.RenameTable(
                name: "CustomerProfiles",
                schema: "identity",
                newName: "CustomerProfiles",
                newSchema: "access");

            migrationBuilder.RenameTable(
                name: "AccessAccounts",
                schema: "identity",
                newName: "AccessAccounts",
                newSchema: "access");

            migrationBuilder.RenameTable(
                name: "IdentityAuditEvents",
                schema: "identity",
                newName: "AccessAuditEvents",
                newSchema: "access");

            migrationBuilder.RenameIndex(
                name: "IX_IdentityAuditEvents_TargetInvitationId",
                schema: "access",
                table: "AccessAuditEvents",
                newName: "IX_AccessAuditEvents_TargetInvitationId");

            migrationBuilder.RenameIndex(
                name: "IX_IdentityAuditEvents_TargetAccountId",
                schema: "access",
                table: "AccessAuditEvents",
                newName: "IX_AccessAuditEvents_TargetAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_IdentityAuditEvents_CreatedAtUtc",
                schema: "access",
                table: "AccessAuditEvents",
                newName: "IX_AccessAuditEvents_CreatedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessAuditEvents",
                schema: "access",
                table: "AccessAuditEvents",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessAuditEvents",
                schema: "access",
                table: "AccessAuditEvents");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.RenameTable(
                name: "StaffInvitations",
                schema: "access",
                newName: "StaffInvitations",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "CustomerProfiles",
                schema: "access",
                newName: "CustomerProfiles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AccessAccounts",
                schema: "access",
                newName: "AccessAccounts",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AccessAuditEvents",
                schema: "access",
                newName: "IdentityAuditEvents",
                newSchema: "identity");

            migrationBuilder.RenameIndex(
                name: "IX_AccessAuditEvents_TargetInvitationId",
                schema: "identity",
                table: "IdentityAuditEvents",
                newName: "IX_IdentityAuditEvents_TargetInvitationId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessAuditEvents_TargetAccountId",
                schema: "identity",
                table: "IdentityAuditEvents",
                newName: "IX_IdentityAuditEvents_TargetAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessAuditEvents_CreatedAtUtc",
                schema: "identity",
                table: "IdentityAuditEvents",
                newName: "IX_IdentityAuditEvents_CreatedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdentityAuditEvents",
                schema: "identity",
                table: "IdentityAuditEvents",
                column: "Id");
        }
    }
}
