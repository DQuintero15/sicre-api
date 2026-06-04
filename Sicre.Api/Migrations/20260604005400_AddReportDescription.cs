using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "reports",
                table: "reports",
                type: "text",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", schema: "reports", table: "reports");
        }
    }
}
