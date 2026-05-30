using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportInstanceNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportInstanceNotes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorRole = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportInstanceNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportInstanceNotes_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ReportInstanceNotes_report_instances_ReportInstanceId",
                        column: x => x.ReportInstanceId,
                        principalSchema: "reports",
                        principalTable: "report_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstanceNotes_AuthorUserId",
                schema: "identity",
                table: "ReportInstanceNotes",
                column: "AuthorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstanceNotes_ReportInstanceId",
                schema: "identity",
                table: "ReportInstanceNotes",
                column: "ReportInstanceId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReportInstanceNotes", schema: "identity");
        }
    }
}
