using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixReportNotificationEmailsColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NotificationEmails was incorrectly mapped as jsonb.
            // It stores a plain comma-separated string, not JSON.
            // Cast existing jsonb values to text (unwraps JSON string literals if any exist).
            migrationBuilder.AlterColumn<string>(
                name: "NotificationEmails",
                schema: "reports",
                table: "reports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NotificationEmails",
                schema: "reports",
                table: "reports",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );
        }
    }
}
