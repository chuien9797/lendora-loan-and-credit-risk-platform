using System.Globalization;
using System.Text.Json;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.Risk;
using Lendora.Application.Loans;
using Lendora.Application.Risk;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Risk;

internal sealed class RiskAssessmentService(ApplicationDbContext dbContext, IApplicationAuditService auditService) : IRiskAssessmentService
{
    public async Task<ServiceResult<RiskAssessmentDto>> GetAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(loanApplicationId, cancellationToken);
        if (application is null)
        {
            return ServiceResult<RiskAssessmentDto>.Failure("Loan application not found.");
        }

        if (!CanAccessApplication(application, userId, roles))
        {
            return ServiceResult<RiskAssessmentDto>.Failure("You do not have access to this risk assessment.");
        }

        if (application.RiskAssessment is null)
        {
            return ServiceResult<RiskAssessmentDto>.Failure("Risk assessment has not been generated yet.");
        }

        return ServiceResult<RiskAssessmentDto>.Success(MapToDto(application.RiskAssessment));
    }

    public async Task<ServiceResult<RiskAssessmentDto>> GenerateAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(loanApplicationId, cancellationToken);
        if (application is null)
        {
            return ServiceResult<RiskAssessmentDto>.Failure("Loan application not found.");
        }

        if (!CanAccessApplication(application, userId, roles))
        {
            return ServiceResult<RiskAssessmentDto>.Failure("You do not have access to this risk assessment.");
        }

        if (application.Status == LoanApplicationStatus.Draft)
        {
            return ServiceResult<RiskAssessmentDto>.Failure("Submit the application before running risk assessment.");
        }

        if (application.AffordabilityAssessment is null)
        {
            return ServiceResult<RiskAssessmentDto>.Failure("Run affordability assessment before risk scoring.");
        }

        var affordability = application.AffordabilityAssessment;
        var calculationInput = new RiskCalculationInput(
            application.CreditScore,
            application.CtosScore,
            application.InternalAccountHistoryScore,
            application.BehaviourScore,
            application.FraudRiskScore,
            application.KycRiskScore,
            application.IncomeVerificationStatus,
            application.MissedPaymentCount,
            application.EmploymentDurationMonths,
            application.NumberOfDependents,
            application.EmploymentStatus,
            application.ResidentialStatus,
            affordability.DebtServiceRatio,
            affordability.DisposableIncome,
            affordability.Result);
        var result = RiskCalculator.Calculate(calculationInput);

        var assessment = application.RiskAssessment;
        if (assessment is null)
        {
            assessment = new RiskAssessment
            {
                LoanApplicationId = application.Id
            };
            dbContext.RiskAssessments.Add(assessment);
        }

        assessment.Score = result.Score;
        assessment.Grade = result.Grade;
        assessment.Recommendation = result.Recommendation;
        assessment.Factors = JsonSerializer.Serialize(result.Factors);
        assessment.AssessedAtUtc = DateTime.UtcNow;
        assessment.UpdatedAtUtc = DateTime.UtcNow;

        if (assessment.Recommendation == RiskAssessmentRecommendation.ManualReview)
        {
            application.Status = LoanApplicationStatus.ManualReview;
            application.UpdatedAtUtc = DateTime.UtcNow;
        }

        await auditService.RecordAsync(
            application.Id,
            userId,
            GetPrimaryStaffRole(roles),
            "RiskGenerated",
            "Staff generated a credit risk assessment.",
            BuildAuditDetails(assessment, calculationInput, result.Factors),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<RiskAssessmentDto>.Success(MapToDto(assessment));
    }

    private async Task<LoanApplication?> LoadApplicationAsync(Guid loanApplicationId, CancellationToken cancellationToken)
    {
        return await dbContext.LoanApplications
            .Include(application => application.AffordabilityAssessment)
            .Include(application => application.RiskAssessment)
            .FirstOrDefaultAsync(application => application.Id == loanApplicationId, cancellationToken);
    }

    private static bool CanAccessApplication(LoanApplication application, Guid userId, IReadOnlyCollection<string> roles)
    {
        return IsStaff(roles);
    }

    private static bool IsStaff(IReadOnlyCollection<string> roles)
    {
        return roles.Contains(ApplicationRoles.Admin) ||
            roles.Contains(ApplicationRoles.LoanOfficer) ||
            roles.Contains(ApplicationRoles.Underwriter);
    }

    private static string GetPrimaryStaffRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(ApplicationRoles.Admin))
        {
            return ApplicationRoles.Admin;
        }

        if (roles.Contains(ApplicationRoles.Underwriter))
        {
            return ApplicationRoles.Underwriter;
        }

        if (roles.Contains(ApplicationRoles.LoanOfficer))
        {
            return ApplicationRoles.LoanOfficer;
        }

        return "Staff";
    }

    private static string BuildAuditDetails(
        RiskAssessment assessment,
        RiskCalculationInput input,
        IReadOnlyCollection<string> factors)
    {
        var keyFactors = factors.Count == 0
            ? "No rule factors returned"
            : string.Join("; ", factors.Take(4));

        return string.Join(" ", [
            $"Score {assessment.Score}, grade {assessment.Grade}, recommendation {assessment.Recommendation}.",
            "Human approval required before any final decision.",
            "Inputs logged:",
            $"credit {FormatNullable(input.CreditScore)}, CTOS {FormatNullable(input.CtosScore)}, behaviour {FormatNullable(input.BehaviourScore)}, fraud {FormatNullable(input.FraudRiskScore)}, KYC {FormatNullable(input.KycRiskScore)}, missed payments {input.MissedPaymentCount}, DSR {input.DebtServiceRatio.ToString("0.##", CultureInfo.InvariantCulture)}%, disposable income RM {input.DisposableIncome.ToString("0.##", CultureInfo.InvariantCulture)}.",
            $"Reasons: {keyFactors}."
        ]);
    }

    private static string FormatNullable(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
    }

    private static RiskAssessmentDto MapToDto(RiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.LoanApplicationId,
            assessment.Score,
            assessment.Grade,
            assessment.Recommendation,
            DeserializeFactors(assessment.Factors),
            assessment.AssessedAtUtc);

    private static IReadOnlyCollection<string> DeserializeFactors(string factors)
    {
        if (string.IsNullOrWhiteSpace(factors))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyCollection<string>>(factors) ?? [];
    }
}
