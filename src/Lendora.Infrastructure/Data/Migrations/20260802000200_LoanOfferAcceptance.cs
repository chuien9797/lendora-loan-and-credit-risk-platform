using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LoanOfferAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.LoanApplications', 'OfferAcceptedAtUtc') IS NULL
                    ALTER TABLE [LoanApplications] ADD [OfferAcceptedAtUtc] datetime2 NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.LoanApplications', 'OfferAcceptedAtUtc') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [OfferAcceptedAtUtc];
                """);
        }
    }
}
