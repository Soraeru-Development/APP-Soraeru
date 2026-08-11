using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soraeru.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WordCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DetectedLanguage = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MeaningZh = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Pronunciation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SelectedMnemonic = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordCards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WordCards_UserId",
                table: "WordCards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WordCards_UserId_DetectedLanguage_NormalizedText",
                table: "WordCards",
                columns: new[] { "UserId", "DetectedLanguage", "NormalizedText" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordCards");
        }
    }
}
