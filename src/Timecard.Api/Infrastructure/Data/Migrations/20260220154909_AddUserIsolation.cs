using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timecard.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Users table first (required before FK and before migrating existing rows).
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");

            // 2. Add UserId column; existing rows default to the dev placeholder.
            migrationBuilder.DropIndex(
                name: "IX_WorkDays_Date",
                table: "WorkDays");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WorkDays",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "dev-placeholder");

            migrationBuilder.CreateIndex(
                name: "IX_WorkDays_UserId_Date",
                table: "WorkDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkDays_Users_UserId",
                table: "WorkDays",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkDays_Users_UserId",
                table: "WorkDays");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_WorkDays_UserId_Date",
                table: "WorkDays");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WorkDays");

            migrationBuilder.CreateIndex(
                name: "IX_WorkDays_Date",
                table: "WorkDays",
                column: "Date",
                unique: true);
        }
    }
}
