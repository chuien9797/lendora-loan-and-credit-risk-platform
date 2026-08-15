using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.PostgresMigrations
{
    /// <inheritdoc />
    public partial class Stage13DocumentOcr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OcrConfidence",
                table: "ApplicationDocuments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrExtractedText",
                table: "ApplicationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrFailureReason",
                table: "ApplicationDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OcrProcessedAtUtc",
                table: "ApplicationDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OcrProcessedByUserId",
                table: "ApplicationDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrProvider",
                table: "ApplicationDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OcrStatus",
                table: "ApplicationDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OcrSuggestedMonthlyExpenses",
                table: "ApplicationDocuments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OcrSuggestedMonthlyIncome",
                table: "ApplicationDocuments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrSummary",
                table: "ApplicationDocuments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_OcrStatus",
                table: "ApplicationDocuments",
                column: "OcrStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationDocuments_OcrStatus",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrExtractedText",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrFailureReason",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrProcessedAtUtc",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrProcessedByUserId",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrProvider",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrStatus",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSuggestedMonthlyExpenses",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSuggestedMonthlyIncome",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSummary",
                table: "ApplicationDocuments");
        }
    }
}
