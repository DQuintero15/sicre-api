using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sicre.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "identity");

            migrationBuilder.EnsureSchema(name: "organization");

            migrationBuilder.EnsureSchema(name: "reports");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "control_entities",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Abbreviation = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: true
                    ),
                    Nit = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: true
                    ),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    LegalBasis = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_entities", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "google_drive_tokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_drive_tokens", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "password_reset_requests",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    TokenHash = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    RequestedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IpAddress = table.Column<string>(
                        type: "character varying(45)",
                        maxLength: 45,
                        nullable: true
                    ),
                    UserAgent = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_requests", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "positions",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "processes",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    LastName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    HasChangedDefaultPassword = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    TwoFactorSecret = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    UserName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedUserName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    Email = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedEmail = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUsers_positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "organization",
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUsers_processes_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "organization",
                        principalTable: "processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AspNetUserLogins",
                        x => new { x.LoginProvider, x.ProviderKey }
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AspNetUserTokens",
                        x => new
                        {
                            x.UserId,
                            x.LoginProvider,
                            x.Name,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    IsRevoked = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ControlEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegalBasis = table.Column<string>(type: "text", nullable: true),
                    Frequency = table.Column<string>(type: "varchar(50)", nullable: false),
                    GenerationMode = table.Column<string>(
                        type: "varchar(50)",
                        nullable: false,
                        defaultValue: "Automatic"
                    ),
                    DueDateRuleType = table.Column<string>(type: "varchar(50)", nullable: false),
                    DueDatePeriodUnit = table.Column<string>(type: "varchar(20)", nullable: true),
                    DueDateDayNumber = table.Column<int>(type: "integer", nullable: true),
                    DueDateDaysToAdd = table.Column<int>(type: "integer", nullable: true),
                    DueDateMonthOffset = table.Column<int>(type: "integer", nullable: true),
                    DueDateYearOffset = table.Column<int>(type: "integer", nullable: true),
                    DueDateFixedMonth = table.Column<int>(type: "integer", nullable: true),
                    DueDateFixedDay = table.Column<int>(type: "integer", nullable: true),
                    DueDateSpecificDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDateFixedDatesDefinition = table.Column<string>(
                        type: "jsonb",
                        nullable: true
                    ),
                    DueDateRangesDefinition = table.Column<string>(type: "jsonb", nullable: true),
                    DueDateExceptionsDefinition = table.Column<string>(
                        type: "jsonb",
                        nullable: true
                    ),
                    OriginalDueDateText = table.Column<string>(type: "text", nullable: true),
                    AlertEarlyDays = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 15
                    ),
                    AlertFollowUpDays = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 5
                    ),
                    AlertCriticalDays = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 1
                    ),
                    FormatTypes = table.Column<string>(type: "jsonb", nullable: false),
                    InstructionsUrl = table.Column<string>(type: "text", nullable: true),
                    TemplateFileUrl = table.Column<string>(type: "text", nullable: true),
                    NotificationEmails = table.Column<string>(type: "jsonb", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SenderResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityUploadResponsibleUserId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false
                    ),
                    FollowUpLeaderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reports_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_AspNetUsers_EntityUploadResponsibleUserId",
                        column: x => x.EntityUploadResponsibleUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_AspNetUsers_FollowUpLeaderUserId",
                        column: x => x.FollowUpLeaderUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_AspNetUsers_SenderResponsibleUserId",
                        column: x => x.SenderResponsibleUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_reports_control_entities_ControlEntityId",
                        column: x => x.ControlEntityId,
                        principalSchema: "organization",
                        principalTable: "control_entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_reports_processes_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "organization",
                        principalTable: "processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "sicre_settings",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoLiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AutoNotify = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sicre_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sicre_settings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "report_instances",
                schema: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: true),
                    PeriodName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SentDate = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    DelayReason = table.Column<string>(type: "text", nullable: true),
                    Observations = table.Column<string>(type: "text", nullable: true),
                    ManualActivationReason = table.Column<string>(type: "text", nullable: true),
                    DueDateOverrideReason = table.Column<string>(type: "text", nullable: true),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupervisorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_instances_AspNetUsers_ActivatedByUserId",
                        column: x => x.ActivatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_instances_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_instances_AspNetUsers_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_instances_AspNetUsers_SupervisorUserId",
                        column: x => x.SupervisorUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_instances_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_instances_reports_ReportId",
                        column: x => x.ReportId,
                        principalSchema: "reports",
                        principalTable: "reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", nullable: false),
                    Severity = table.Column<string>(type: "varchar(20)", nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", nullable: false),
                    Readed = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                    ReportInstanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_notifications_report_instances_ReportInstanceId",
                        column: x => x.ReportInstanceId,
                        principalSchema: "reports",
                        principalTable: "report_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "report_attachments",
                schema: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "varchar(50)", nullable: false),
                    FileName = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    MimeType = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    GoogleFileId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    WebViewLink = table.Column<string>(type: "text", nullable: true),
                    WebContentLink = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_attachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_attachments_report_instances_ReportInstanceId",
                        column: x => x.ReportInstanceId,
                        principalSchema: "reports",
                        principalTable: "report_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "report_reversions",
                schema: "reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(50)", nullable: false),
                    NewStatus = table.Column<string>(type: "varchar(50)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamptz",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_reversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_reversions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_report_reversions_report_instances_ReportInstanceId",
                        column: x => x.ReportInstanceId,
                        principalSchema: "reports",
                        principalTable: "report_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "identity",
                table: "AspNetRoleClaims",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "identity",
                table: "AspNetUserClaims",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "identity",
                table: "AspNetUserLogins",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "identity",
                table: "AspNetUserRoles",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedEmail"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PositionId",
                schema: "identity",
                table: "AspNetUsers",
                column: "PositionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ProcessId",
                schema: "identity",
                table: "AspNetUsers",
                column: "ProcessId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                schema: "identity",
                table: "AspNetUsers",
                column: "BranchId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "identity",
                table: "AspNetUsers",
                column: "IsActive"
            );

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Name",
                schema: "organization",
                table: "branches",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ControlEntities_Abbreviation",
                schema: "organization",
                table: "control_entities",
                column: "Abbreviation"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ControlEntities_IsActive",
                schema: "organization",
                table: "control_entities",
                column: "IsActive"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReportInstance",
                schema: "identity",
                table: "notifications",
                column: "ReportInstanceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Created",
                schema: "identity",
                table: "notifications",
                columns: new[] { "UserId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Readed_Created",
                schema: "identity",
                table: "notifications",
                columns: new[] { "UserId", "Readed", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_Email_Used",
                schema: "identity",
                table: "password_reset_requests",
                columns: new[] { "Email", "UsedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_Token",
                schema: "identity",
                table: "password_reset_requests",
                column: "TokenHash"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Name",
                schema: "organization",
                table: "positions",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Processes_Name",
                schema: "organization",
                table: "processes",
                column: "Name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "identity",
                table: "refresh_tokens",
                column: "Token",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_User_Active",
                schema: "identity",
                table: "refresh_tokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_report_attachments_UploadedByUserId",
                schema: "reports",
                table: "report_attachments",
                column: "UploadedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportAttachments_GoogleFileId",
                schema: "reports",
                table: "report_attachments",
                column: "GoogleFileId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportAttachments_Instance_Type",
                schema: "reports",
                table: "report_attachments",
                columns: new[] { "ReportInstanceId", "Type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_report_instances_ActivatedByUserId",
                schema: "reports",
                table: "report_instances",
                column: "ActivatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_report_instances_CreatedByUserId",
                schema: "reports",
                table: "report_instances",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_report_instances_UpdatedByUserId",
                schema: "reports",
                table: "report_instances",
                column: "UpdatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_DueDate",
                schema: "reports",
                table: "report_instances",
                column: "DueDate"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Period_Report",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "PeriodYear", "PeriodMonth", "ReportId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Responsible_Status_Due",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "ResponsibleUserId", "Status", "DueDate" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Status_DueDate",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "Status", "DueDate" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Supervisor_Status",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "SupervisorUserId", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Unique_Automatic",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "ReportId", "PeriodYear", "PeriodMonth" },
                unique: true,
                filter: "\"EventDate\" IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportInstances_Unique_Manual",
                schema: "reports",
                table: "report_instances",
                columns: new[] { "ReportId", "EventDate" },
                unique: true,
                filter: "\"EventDate\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_report_reversions_CreatedByUserId",
                schema: "reports",
                table: "report_reversions",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReportReversions_InstanceId",
                schema: "reports",
                table: "report_reversions",
                column: "ReportInstanceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_BranchId",
                schema: "reports",
                table: "reports",
                column: "BranchId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Code_ControlEntity_Branch",
                schema: "reports",
                table: "reports",
                columns: new[] { "Code", "ControlEntityId", "BranchId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ControlEntity_Active_Frequency",
                schema: "reports",
                table: "reports",
                columns: new[] { "ControlEntityId", "IsActive", "Frequency" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_CreatedByUserId",
                schema: "reports",
                table: "reports",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_EntityUploadResponsibleUserId",
                schema: "reports",
                table: "reports",
                column: "EntityUploadResponsibleUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_FollowUpLeaderUserId",
                schema: "reports",
                table: "reports",
                column: "FollowUpLeaderUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Frequency",
                schema: "reports",
                table: "reports",
                column: "Frequency"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GenerationMode",
                schema: "reports",
                table: "reports",
                column: "GenerationMode"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Reports_IsActive",
                schema: "reports",
                table: "reports",
                column: "IsActive"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_ProcessId",
                schema: "reports",
                table: "reports",
                column: "ProcessId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_SenderResponsibleUserId",
                schema: "reports",
                table: "reports",
                column: "SenderResponsibleUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_reports_UpdatedByUserId",
                schema: "reports",
                table: "reports",
                column: "UpdatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_sicre_settings_UpdatedByUserId",
                schema: "identity",
                table: "sicre_settings",
                column: "UpdatedByUserId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AspNetRoleClaims", schema: "identity");

            migrationBuilder.DropTable(name: "AspNetUserClaims", schema: "identity");

            migrationBuilder.DropTable(name: "AspNetUserLogins", schema: "identity");

            migrationBuilder.DropTable(name: "AspNetUserRoles", schema: "identity");

            migrationBuilder.DropTable(name: "AspNetUserTokens", schema: "identity");

            migrationBuilder.DropTable(name: "google_drive_tokens", schema: "identity");

            migrationBuilder.DropTable(name: "notifications", schema: "identity");

            migrationBuilder.DropTable(name: "password_reset_requests", schema: "identity");

            migrationBuilder.DropTable(name: "refresh_tokens", schema: "identity");

            migrationBuilder.DropTable(name: "report_attachments", schema: "reports");

            migrationBuilder.DropTable(name: "report_reversions", schema: "reports");

            migrationBuilder.DropTable(name: "sicre_settings", schema: "identity");

            migrationBuilder.DropTable(name: "AspNetRoles", schema: "identity");

            migrationBuilder.DropTable(name: "report_instances", schema: "reports");

            migrationBuilder.DropTable(name: "reports", schema: "reports");

            migrationBuilder.DropTable(name: "AspNetUsers", schema: "identity");

            migrationBuilder.DropTable(name: "control_entities", schema: "organization");

            migrationBuilder.DropTable(name: "branches", schema: "organization");

            migrationBuilder.DropTable(name: "positions", schema: "organization");

            migrationBuilder.DropTable(name: "processes", schema: "organization");
        }
    }
}
