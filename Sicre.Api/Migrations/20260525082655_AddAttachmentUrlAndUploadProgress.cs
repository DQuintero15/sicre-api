using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentUrlAndUploadProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadProgress",
                schema: "reports",
                table: "report_attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "Url",
                schema: "reports",
                table: "report_attachments",
                type: "text",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadProgress",
                schema: "reports",
                table: "report_attachments"
            );

            migrationBuilder.DropColumn(
                name: "Url",
                schema: "reports",
                table: "report_attachments"
            );
        }
    }
}
