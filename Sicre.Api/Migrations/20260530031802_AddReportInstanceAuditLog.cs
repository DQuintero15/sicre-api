using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportInstanceAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportInstanceAuditEntries",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    HumanReadable = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportInstanceAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportInstanceAuditEntries_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ReportInstanceAuditEntries_report_instances_ReportInstanceId",
                        column: x => x.ReportInstanceId,
                        principalSchema: "reports",
                        principalTable: "report_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstanceAuditEntries_PerformedByUserId",
                schema: "identity",
                table: "ReportInstanceAuditEntries",
                column: "PerformedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstanceAuditEntries_ReportInstanceId",
                schema: "identity",
                table: "ReportInstanceAuditEntries",
                column: "ReportInstanceId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReportInstanceAuditEntries", schema: "identity");
        }
    }
}
