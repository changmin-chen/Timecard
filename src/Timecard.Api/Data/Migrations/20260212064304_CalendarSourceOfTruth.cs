using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timecard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalendarSourceOfTruth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarDayOverrides",
                columns: table => new
                {
                    CalendarId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    IsWorking = table.Column<bool>(type: "boolean", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDayOverrides", x => new { x.CalendarId, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "CalendarDays",
                columns: table => new
                {
                    CalendarId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<string>(type: "text", nullable: false),
                    IsWorking = table.Column<bool>(type: "boolean", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDays", x => new { x.CalendarId, x.Date });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDayOverrides_Date",
                table: "CalendarDayOverrides",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDays_Date",
                table: "CalendarDays",
                column: "Date");

            migrationBuilder.Sql("""
                INSERT INTO "CalendarDayOverrides"
                    ("CalendarId", "Date", "IsWorking", "Kind", "Note", "Source", "UpdatedAt")
                SELECT
                    'TW_DGPA',
                    "Date",
                    NOT "IsNonWorkingDay",
                    CASE
                        WHEN "IsNonWorkingDay" THEN 'LegacyManualHoliday'
                        ELSE 'LegacyManualWorkingDay'
                    END,
                    COALESCE("Note", ''),
                    'LegacyWorkDay',
                    CURRENT_TIMESTAMP
                FROM "WorkDays"
                WHERE "IsNonWorkingDay" = TRUE OR COALESCE("Note", '') <> '';
                """);

            migrationBuilder.DropColumn(
                name: "IsNonWorkingDay",
                table: "WorkDays");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "WorkDays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNonWorkingDay",
                table: "WorkDays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "WorkDays",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "WorkDays" w
                SET
                    "IsNonWorkingDay" = NOT o."IsWorking",
                    "Note" = COALESCE(o."Note", '')
                FROM "CalendarDayOverrides" o
                WHERE
                    o."CalendarId" = 'TW_DGPA'
                    AND o."Date" = w."Date"
                    AND (o."Source" = 'ManualOverride' OR o."Source" = 'LegacyWorkDay');
                """);

            migrationBuilder.DropTable(
                name: "CalendarDayOverrides");

            migrationBuilder.DropTable(
                name: "CalendarDays");
        }
    }
}
