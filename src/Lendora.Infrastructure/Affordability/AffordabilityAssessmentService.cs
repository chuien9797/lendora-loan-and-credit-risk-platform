using Lendora.Application.Abstractions.Affordability;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Affordability;
using Lendora.Application.Loans;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Affordability;

internal sealed class AffordabilityAssessmentService(ApplicationDbContext dbContext, IApplicationAuditService auditService) : IAffordabilityAssessmentService
{
    public async Task<ServiceResult<AffordabilityAssessmentDto>> GetAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(loanApplicationId, cancellationToken);
        if (application is null)
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("Loan application not found.");
        }

        if (!CanAccessApplication(application, userId, roles))
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("You do not have access to this affordability assessment.");
        }

        if (application.AffordabilityAssessment is null)
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("Affordability assessment has not been generated yet.");
        }

        return ServiceResult<AffordabilityAssessmentDto>.Success(MapToDto(application.AffordabilityAssessment));
    }

    public async Task<ServiceResult<AffordabilityAssessmentDto>> GenerateAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(loanApplicationId, cancellationToken);
        if (application is null)
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("Loan application not found.");
        }

        if (!CanAccessApplication(application, userId, roles))
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("You do not have access to this affordability assessment.");
        }

        if (application.Status == LoanApplicationStatus.Draft)
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("Submit the application before running affordability assessment.");
        }

        if (application.LoanProduct is null)
        {
            return ServiceResult<AffordabilityAssessmentDto>.Failure("Loan product is required before running affordability assessment.");
        }

        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            application.LoanAmount,
            application.LoanTermMonths,
            application.LoanProduct.InterestRate,
            application.MonthlyIncome,
            application.MonthlyExpenses,
            application.ExistingMonthlyDebt));

        var assessment = application.AffordabilityAssessment;
        if (assessment is null)
        {
            assessment = new AffordabilityAssessment
            {
                LoanApplicationId = application.Id
            };
            dbContext.AffordabilityAssessments.Add(assessment);
        }

        assessment.MonthlyRepayment = result.MonthlyRepayment;
        assessment.TotalRepayment = result.TotalRepayment;
        assessment.TotalInterest = result.TotalInterest;
        assessment.DebtServiceRatio = result.DebtServiceRatio;
        assessment.DisposableIncome = result.DisposableIncome;
        assessment.Result = result.Result;
        assessment.AssessedAtUtc = DateTime.UtcNow;
        assessment.UpdatedAtUtc = DateTime.UtcNow;

        if (application.Status == LoanApplicationStatus.Submitted)
        {
            application.Status = LoanApplicationStatus.AssessmentInProgress;
            application.UpdatedAtUtc = DateTime.UtcNow;
        }

        await auditService.RecordAsync(
            application.Id,
            userId,
            GetPrimaryStaffRole(roles),
            "AffordabilityGenerated",
            "Staff generated an affordability assessment.",
            $"Monthly repayment: {assessment.MonthlyRepayment:0.##}. DSR: {assessment.DebtServiceRatio:0.##}%. Result: {assessment.Result}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<AffordabilityAssessmentDto>.Success(MapToDto(assessment));
    }

    private async Task<LoanApplication?> LoadApplicationAsync(Guid loanApplicationId, CancellationToken cancellationToken)
    {
        return await dbContext.LoanApplications
            .Include(application => application.LoanProduct)
            .Include(application => application.AffordabilityAssessment)
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

    private static AffordabilityAssessmentDto MapToDto(AffordabilityAssessment assessment) =>
        new(
            assessment.Id,
            assessment.LoanApplicationId,
            assessment.MonthlyRepayment,
            assessment.TotalRepayment,
            assessment.TotalInterest,
            assessment.DebtServiceRatio,
            assessment.DisposableIncome,
            assessment.Result,
            assessment.AssessedAtUtc);
}
