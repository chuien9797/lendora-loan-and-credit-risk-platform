using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BankScoringAndLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CreditScore",
                table: "LoanApplications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedLimit",
                table: "LoanApplications",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BehaviourScore",
                table: "LoanApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreditScoreCheckedAtUtc",
                table: "LoanApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditScoreSource",
                table: "LoanApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCreditCheckConsent",
                table: "LoanApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLimitLocked",
                table: "LoanApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LimitDecisionReason",
                table: "LoanApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LimitReviewedAtUtc",
                table: "LoanApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LimitReviewedByUserId",
                table: "LoanApplications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MissedPaymentCount",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RecommendedInitialLimit",
                table: "LoanApplications",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ApprovedLimit", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "BehaviourScore", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "CreditScoreCheckedAtUtc", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "CreditScoreSource", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "HasCreditCheckConsent", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "IsLimitLocked", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "LimitDecisionReason", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "LimitReviewedAtUtc", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "LimitReviewedByUserId", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "MissedPaymentCount", table: "LoanApplications");
            migrationBuilder.DropColumn(name: "RecommendedInitialLimit", table: "LoanApplications");

            migrationBuilder.AlterColumn<int>(
                name: "CreditScore",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
