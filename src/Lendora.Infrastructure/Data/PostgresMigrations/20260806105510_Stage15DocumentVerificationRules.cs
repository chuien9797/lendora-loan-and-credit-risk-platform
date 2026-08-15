using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Stage15DocumentVerificationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OcrDocumentDate",
                table: "ApplicationDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OcrIsRecent",
                table: "ApplicationDocuments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrVerificationFindings",
                table: "ApplicationDocuments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrVerificationStatus",
                table: "ApplicationDocuments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrDocumentDate",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrIsRecent",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrVerificationFindings",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrVerificationStatus",
                table: "ApplicationDocuments");
        }
    }
}
