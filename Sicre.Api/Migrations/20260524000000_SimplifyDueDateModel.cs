using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDueDateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove obsolete columns
            migrationBuilder.DropColumn(
                name: "DueDatePeriodUnit",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateDayNumber",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateMonthOffset",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateYearOffset",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateFixedMonth",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateFixedDay",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateSpecificDate",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateRangesDefinition",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateExceptionsDefinition",
                schema: "reports",
                table: "reports"
            );
            migrationBuilder.DropColumn(
                name: "DueDateDaysToAdd",
                schema: "reports",
                table: "reports"
            );

            // Rename DueDateFixedDatesDefinition -> DueDateDatesDefinition
            migrationBuilder.RenameColumn(
                name: "DueDateFixedDatesDefinition",
                schema: "reports",
                table: "reports",
                newName: "DueDateDatesDefinition"
            );

            // Add new simplified columns
            migrationBuilder.AddColumn<int>(
                name: "DueDateDay",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateMonth",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DueDateDay", schema: "reports", table: "reports");
            migrationBuilder.DropColumn(name: "DueDateMonth", schema: "reports", table: "reports");

            migrationBuilder.RenameColumn(
                name: "DueDateDatesDefinition",
                schema: "reports",
                table: "reports",
                newName: "DueDateFixedDatesDefinition"
            );

            migrationBuilder.AddColumn<string>(
                name: "DueDatePeriodUnit",
                schema: "reports",
                table: "reports",
                type: "varchar(20)",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateDayNumber",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateMonthOffset",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateYearOffset",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateFixedMonth",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateFixedDay",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
            migrationBuilder.AddColumn<System.DateOnly>(
                name: "DueDateSpecificDate",
                schema: "reports",
                table: "reports",
                type: "date",
                nullable: true
            );
            migrationBuilder.AddColumn<string>(
                name: "DueDateRangesDefinition",
                schema: "reports",
                table: "reports",
                type: "jsonb",
                nullable: true
            );
            migrationBuilder.AddColumn<string>(
                name: "DueDateExceptionsDefinition",
                schema: "reports",
                table: "reports",
                type: "jsonb",
                nullable: true
            );
            migrationBuilder.AddColumn<int>(
                name: "DueDateDaysToAdd",
                schema: "reports",
                table: "reports",
                type: "integer",
                nullable: true
            );
        }
    }
}
