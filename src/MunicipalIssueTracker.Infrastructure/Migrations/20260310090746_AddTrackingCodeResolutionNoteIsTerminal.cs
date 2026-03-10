using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalIssueTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingCodeResolutionNoteIsTerminal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTerminal",
                table: "Statuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Issues",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "Issues",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_TrackingCode",
                table: "Issues",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Issues_TrackingCode",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "IsTerminal",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "Issues");
        }
    }
}
