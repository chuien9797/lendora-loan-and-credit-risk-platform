using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Stage14DocumentEvidenceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OcrNationalIdMatchesApplication",
                table: "ApplicationDocuments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrSuggestedAddress",
                table: "ApplicationDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrSuggestedNationalIdNumber",
                table: "ApplicationDocuments",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubmittedToBank",
                table: "ApplicationDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrNationalIdMatchesApplication",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSuggestedAddress",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSuggestedNationalIdNumber",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "SubmittedToBank",
                table: "ApplicationDocuments");
        }
    }
}
