using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soraeru.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedMnemonics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerifiedMnemonics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedSource = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NotationText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedMnemonics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedMnemonics_Language_NormalizedSource",
                table: "VerifiedMnemonics",
                columns: new[] { "Language", "NormalizedSource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedMnemonics_Language_NormalizedSource_IsEnabled",
                table: "VerifiedMnemonics",
                columns: new[] { "Language", "NormalizedSource", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifiedMnemonics");
        }
    }
}
