using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationPhoneAndStaffSearchControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.LoanApplications', 'PhoneNumber') IS NULL
                    ALTER TABLE [LoanApplications] ADD [PhoneNumber] nvarchar(40) NOT NULL CONSTRAINT [DF_LoanApplications_PhoneNumber] DEFAULT N'';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.LoanApplications', 'PhoneNumber') IS NOT NULL
                BEGIN
                    IF OBJECT_ID('dbo.DF_LoanApplications_PhoneNumber', 'D') IS NOT NULL
                        ALTER TABLE [LoanApplications] DROP CONSTRAINT [DF_LoanApplications_PhoneNumber];

                    ALTER TABLE [LoanApplications] DROP COLUMN [PhoneNumber];
                END
                """);
        }
    }
}
