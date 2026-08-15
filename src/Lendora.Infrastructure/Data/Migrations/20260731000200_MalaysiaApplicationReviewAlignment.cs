using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lendora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MalaysiaApplicationReviewAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.AffordabilityAssessments', 'DebtToIncomeRatio') IS NOT NULL
                   AND COL_LENGTH('dbo.AffordabilityAssessments', 'DebtServiceRatio') IS NULL
                BEGIN
                    EXEC sp_rename N'dbo.AffordabilityAssessments.DebtToIncomeRatio', N'DebtServiceRatio', N'COLUMN';
                END

                IF COL_LENGTH('dbo.LoanApplications', 'HasCreditCheckConsent') IS NOT NULL
                BEGIN
                    ALTER TABLE [LoanApplications] DROP COLUMN [HasCreditCheckConsent];
                END

                IF COL_LENGTH('dbo.LoanApplications', 'CcrisRecordSummary') IS NULL
                    ALTER TABLE [LoanApplications] ADD [CcrisRecordSummary] nvarchar(1000) NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'CtosScore') IS NULL
                    ALTER TABLE [LoanApplications] ADD [CtosScore] int NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'DecisionNote') IS NULL
                    ALTER TABLE [LoanApplications] ADD [DecisionNote] nvarchar(1000) NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'DecisionedAtUtc') IS NULL
                    ALTER TABLE [LoanApplications] ADD [DecisionedAtUtc] datetime2 NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'DecisionedByUserId') IS NULL
                    ALTER TABLE [LoanApplications] ADD [DecisionedByUserId] uniqueidentifier NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'EmployerOrBusinessName') IS NULL
                    ALTER TABLE [LoanApplications] ADD [EmployerOrBusinessName] nvarchar(200) NOT NULL CONSTRAINT [DF_LoanApplications_EmployerOrBusinessName] DEFAULT N'';

                IF COL_LENGTH('dbo.LoanApplications', 'EmployerOrBusinessRegistrationNumber') IS NULL
                    ALTER TABLE [LoanApplications] ADD [EmployerOrBusinessRegistrationNumber] nvarchar(100) NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'FraudRiskScore') IS NULL
                    ALTER TABLE [LoanApplications] ADD [FraudRiskScore] int NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'HasCreditHistoryConsent') IS NULL
                    ALTER TABLE [LoanApplications] ADD [HasCreditHistoryConsent] bit NOT NULL CONSTRAINT [DF_LoanApplications_HasCreditHistoryConsent] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.LoanApplications', 'HasIncomeVerificationConsent') IS NULL
                    ALTER TABLE [LoanApplications] ADD [HasIncomeVerificationConsent] bit NOT NULL CONSTRAINT [DF_LoanApplications_HasIncomeVerificationConsent] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.LoanApplications', 'HasPersonalDataProcessingConsent') IS NULL
                    ALTER TABLE [LoanApplications] ADD [HasPersonalDataProcessingConsent] bit NOT NULL CONSTRAINT [DF_LoanApplications_HasPersonalDataProcessingConsent] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.LoanApplications', 'IncomeVerificationStatus') IS NULL
                    ALTER TABLE [LoanApplications] ADD [IncomeVerificationStatus] nvarchar(100) NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'InternalAccountHistoryScore') IS NULL
                    ALTER TABLE [LoanApplications] ADD [InternalAccountHistoryScore] int NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'KycRiskScore') IS NULL
                    ALTER TABLE [LoanApplications] ADD [KycRiskScore] int NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'NationalIdNumber') IS NULL
                    ALTER TABLE [LoanApplications] ADD [NationalIdNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_LoanApplications_NationalIdNumber] DEFAULT N'';

                IF COL_LENGTH('dbo.LoanApplications', 'OfferedAmount') IS NULL
                    ALTER TABLE [LoanApplications] ADD [OfferedAmount] decimal(18,2) NULL;

                IF COL_LENGTH('dbo.LoanApplications', 'OfferedTermMonths') IS NULL
                    ALTER TABLE [LoanApplications] ADD [OfferedTermMonths] int NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.LoanApplications', 'CcrisRecordSummary') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [CcrisRecordSummary];
                IF COL_LENGTH('dbo.LoanApplications', 'CtosScore') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [CtosScore];
                IF COL_LENGTH('dbo.LoanApplications', 'DecisionNote') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [DecisionNote];
                IF COL_LENGTH('dbo.LoanApplications', 'DecisionedAtUtc') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [DecisionedAtUtc];
                IF COL_LENGTH('dbo.LoanApplications', 'DecisionedByUserId') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [DecisionedByUserId];
                IF COL_LENGTH('dbo.LoanApplications', 'EmployerOrBusinessName') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [EmployerOrBusinessName];
                IF COL_LENGTH('dbo.LoanApplications', 'EmployerOrBusinessRegistrationNumber') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [EmployerOrBusinessRegistrationNumber];
                IF COL_LENGTH('dbo.LoanApplications', 'FraudRiskScore') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [FraudRiskScore];
                IF COL_LENGTH('dbo.LoanApplications', 'HasCreditHistoryConsent') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [HasCreditHistoryConsent];
                IF COL_LENGTH('dbo.LoanApplications', 'HasIncomeVerificationConsent') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [HasIncomeVerificationConsent];
                IF COL_LENGTH('dbo.LoanApplications', 'HasPersonalDataProcessingConsent') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [HasPersonalDataProcessingConsent];
                IF COL_LENGTH('dbo.LoanApplications', 'IncomeVerificationStatus') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [IncomeVerificationStatus];
                IF COL_LENGTH('dbo.LoanApplications', 'InternalAccountHistoryScore') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [InternalAccountHistoryScore];
                IF COL_LENGTH('dbo.LoanApplications', 'KycRiskScore') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [KycRiskScore];
                IF COL_LENGTH('dbo.LoanApplications', 'NationalIdNumber') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [NationalIdNumber];
                IF COL_LENGTH('dbo.LoanApplications', 'OfferedAmount') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [OfferedAmount];
                IF COL_LENGTH('dbo.LoanApplications', 'OfferedTermMonths') IS NOT NULL
                    ALTER TABLE [LoanApplications] DROP COLUMN [OfferedTermMonths];

                IF COL_LENGTH('dbo.LoanApplications', 'HasCreditCheckConsent') IS NULL
                    ALTER TABLE [LoanApplications] ADD [HasCreditCheckConsent] bit NOT NULL CONSTRAINT [DF_LoanApplications_HasCreditCheckConsent] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.AffordabilityAssessments', 'DebtServiceRatio') IS NOT NULL
                   AND COL_LENGTH('dbo.AffordabilityAssessments', 'DebtToIncomeRatio') IS NULL
                BEGIN
                    EXEC sp_rename N'dbo.AffordabilityAssessments.DebtServiceRatio', N'DebtToIncomeRatio', N'COLUMN';
                END
                """);
        }
    }
}
