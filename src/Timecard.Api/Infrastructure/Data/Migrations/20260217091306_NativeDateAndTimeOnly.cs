using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timecard.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NativeDateAndTimeOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "WorkDays"
                ALTER COLUMN "Date" TYPE date
                USING "Date"::date;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CalendarDays"
                ALTER COLUMN "Date" TYPE date
                USING "Date"::date;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CalendarDayOverrides"
                ALTER COLUMN "Date" TYPE date
                USING "Date"::date;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceRequests"
                ALTER COLUMN "Start" TYPE time without time zone
                USING "Start"::time;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceRequests"
                ALTER COLUMN "End" TYPE time without time zone
                USING "End"::time;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AttendanceRequests_StartBeforeEnd",
                table: "AttendanceRequests",
                sql: "\"End\" > \"Start\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AttendanceRequests_StartBeforeEnd",
                table: "AttendanceRequests");

            migrationBuilder.Sql("""
                ALTER TABLE "WorkDays"
                ALTER COLUMN "Date" TYPE text
                USING to_char("Date", 'YYYY-MM-DD');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CalendarDays"
                ALTER COLUMN "Date" TYPE text
                USING to_char("Date", 'YYYY-MM-DD');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CalendarDayOverrides"
                ALTER COLUMN "Date" TYPE text
                USING to_char("Date", 'YYYY-MM-DD');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceRequests"
                ALTER COLUMN "Start" TYPE text
                USING to_char("Start", 'HH24:MI');
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "AttendanceRequests"
                ALTER COLUMN "End" TYPE text
                USING to_char("End", 'HH24:MI');
                """);
        }
    }
}
