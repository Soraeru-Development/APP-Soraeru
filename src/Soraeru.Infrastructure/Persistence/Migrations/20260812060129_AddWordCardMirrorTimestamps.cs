using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soraeru.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordCardMirrorTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "WordCards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "WordCards",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Existing rows: treat CreatedAt as first-seen UpdatedAt for initial App pull.
            migrationBuilder.Sql("""UPDATE "WordCards" SET "UpdatedAt" = "CreatedAt";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WordCards");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WordCards");
        }
    }
}
