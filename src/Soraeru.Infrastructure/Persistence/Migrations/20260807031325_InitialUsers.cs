using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soraeru.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageDaily",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AnalyzeCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDaily", x => new { x.UserId, x.UsageDate });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    GoogleSubject = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PlanTier = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DailyQuota = table.Column<int>(type: "INTEGER", nullable: false),
                    NotationPref = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsDeveloper = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnboardingCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageDaily");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
