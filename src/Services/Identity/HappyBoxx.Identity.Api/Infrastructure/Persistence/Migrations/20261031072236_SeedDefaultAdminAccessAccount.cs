using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyBoxx.Identity.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdminAccessAccount : Migration
    {
        private const string AccountId = "01A1867A-6A9C-73C6-9EA7-9EE359C38A35";
        private const string ExternalSubject = "1GgalvCP7ZudMybvmRmLJ3Ah-6I18FQeh7X3Ai1_aM";
        private const string Email = "danielvo00.au@gmail.com";
        private const string NormalizedEmail = "DANIELVO00.AU@GMAIL.COM";
        private const string DisplayName = "Daniel Vo";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [access].[AccessAccounts] WHERE [NormalizedEmail] = '{NormalizedEmail}')
                   AND NOT EXISTS (SELECT 1 FROM [access].[AccessAccounts] WHERE [ExternalSubject] = '{ExternalSubject}')
                BEGIN
                    INSERT INTO [access].[AccessAccounts]
                        ([Id], [ExternalSubject], [Email], [NormalizedEmail], [DisplayName], [Kind], [Role], [Status], [CreatedAtUtc], [DisabledAtUtc])
                    VALUES
                        ('{AccountId}', '{ExternalSubject}', '{Email}', '{NormalizedEmail}', '{DisplayName}', 1, 'Admin', 1, TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'), NULL);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM [access].[AccessAccounts]
                WHERE [Id] = '{AccountId}'
                  AND [ExternalSubject] = '{ExternalSubject}'
                  AND [NormalizedEmail] = '{NormalizedEmail}';
                """);
        }
    }
}
