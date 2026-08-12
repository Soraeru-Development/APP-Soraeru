using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soraeru.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordRegenerations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WordRegenerations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    NormalizedText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RegenerationCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordRegenerations", x => new { x.UserId, x.SourceLanguage, x.NormalizedText });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WordRegenerations");
        }
    }
}
