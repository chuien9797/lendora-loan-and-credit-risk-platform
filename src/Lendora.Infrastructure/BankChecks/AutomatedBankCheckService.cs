using System.Text.Json;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.BankChecks;
using Lendora.Application.Affordability;
using Lendora.Application.BankChecks;
using Lendora.Application.Loans;
using Lendora.Application.Risk;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.BankChecks;

internal sealed class AutomatedBankCheckService(ApplicationDbContext dbContext, IApplicationAuditService auditService) : IAutomatedBankCheckService
{
    public async Task<ServiceResult<AutomatedBankCheckDto>> RunAsync(Guid applicationId, Guid? reviewedByUserId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(candidate => candidate.LoanProduct)
            .Include(candidate => candidate.AffordabilityAssessment)
            .Include(candidate => candidate.RiskAssessment)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<AutomatedBankCheckDto>.Failure("Loan application not found.");
        }

        if (application.Status == LoanApplicationStatus.Draft)
        {
            return ServiceResult<AutomatedBankCheckDto>.Failure("Submit the application before running automated bank checks.");
        }

        if (application.Status is LoanApplicationStatus.Cancelled or LoanApplicationStatus.Frozen or LoanApplicationStatus.OfferAccepted)
        {
            return ServiceResult<AutomatedBankCheckDto>.Failure("Automated bank checks cannot run on cancelled, frozen, or accepted applications.");
        }

        if (application.LoanProduct is null)
        {
            return ServiceResult<AutomatedBankCheckDto>.Failure("Loan product is required before running automated bank checks.");
        }

        var consentErrors = ValidateConsents(application);
        if (consentErrors.Count > 0)
        {
            return ServiceResult<AutomatedBankCheckDto>.Failure(consentErrors.ToArray());
        }

        var providerResult = MockBankCheckProvider.Generate(application);
        ApplyProviderResult(application, providerResult, reviewedByUserId);

        var affordability = ApplyAffordability(application);
        var risk = ApplyRisk(application, affordability);

        if (application.Status is not LoanApplicationStatus.Approved and not LoanApplicationStatus.Rejected)
        {
            application.Status = LoanApplicationStatus.ManualReview;
        }

        application.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            application.Id,
            reviewedByUserId,
            reviewedByUserId.HasValue ? "Staff" : "System",
            "AutomatedChecksRun",
            "Automated bank checks, affordability, and risk scoring completed.",
            $"Credit score: {application.CreditScore}. Recommendation: {risk.Recommendation}. Status: {application.Status}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<AutomatedBankCheckDto>.Success(new AutomatedBankCheckDto(
            MapApplication(application, includeBankOnlyFields: true),
            MapAffordability(affordability),
            MapRisk(risk),
            providerResult.ProviderNotes));
    }

    private static List<string> ValidateConsents(LoanApplication application)
    {
        var errors = new List<string>();

        if (!application.HasCreditHistoryConsent)
        {
            errors.Add("Credit history consent is required before automated bank checks.");
        }

        if (!application.HasIncomeVerificationConsent)
        {
            errors.Add("Income verification consent is required before automated bank checks.");
        }

        if (!application.HasPersonalDataProcessingConsent)
        {
            errors.Add("Personal data processing consent is required before automated bank checks.");
        }

        return errors;
    }

    private static void ApplyProviderResult(LoanApplication application, MockBankCheckResult result, Guid? reviewedByUserId)
    {
        application.CreditScore = result.CreditScore;
        application.CreditScoreSource = result.CreditScoreSource;
        application.CreditScoreCheckedAtUtc = DateTime.UtcNow;
        application.CcrisRecordSummary = result.CcrisRecordSummary;
        application.CtosScore = result.CtosScore;
        application.InternalAccountHistoryScore = result.InternalAccountHistoryScore;
        application.BehaviourScore = result.BehaviourScore;
        application.FraudRiskScore = result.FraudRiskScore;
        application.KycRiskScore = result.KycRiskScore;
        application.IncomeVerificationStatus = result.IncomeVerificationStatus;
        application.MissedPaymentCount = result.MissedPaymentCount;
        application.RecommendedInitialLimit = CalculateRecommendedInitialLimit(application);
        application.ApprovedLimit ??= application.RecommendedInitialLimit;
        application.IsLimitLocked = result.ShouldLockOrReduceLimit;
        application.LimitDecisionReason = result.LimitDecisionReason;
        application.LimitReviewedAtUtc = DateTime.UtcNow;
        application.LimitReviewedByUserId = reviewedByUserId;
    }

    private AffordabilityAssessment ApplyAffordability(LoanApplication application)
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            application.LoanAmount,
            application.LoanTermMonths,
            application.LoanProduct!.InterestRate,
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

        return assessment;
    }

    private RiskAssessment ApplyRisk(LoanApplication application, AffordabilityAssessment affordability)
    {
        var result = RiskCalculator.Calculate(new RiskCalculationInput(
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
            affordability.Result));

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

        return assessment;
    }

    private static decimal CalculateRecommendedInitialLimit(LoanApplication application)
    {
        var incomeBasedLimit = application.MonthlyIncome * 2m;
        var requestBasedLimit = application.LoanAmount * 0.3m;
        var baseLimit = Math.Min(incomeBasedLimit, requestBasedLimit);

        if (!application.CreditScore.HasValue ||
            application.CreditScore.Value < 620 ||
            application.BehaviourScore is < 45 ||
            application.InternalAccountHistoryScore is < 45 ||
            application.FraudRiskScore is > 50 ||
            application.KycRiskScore is > 50 ||
            application.MissedPaymentCount > 0)
        {
            baseLimit *= 0.5m;
        }

        if (application.CreditScore.HasValue && application.CreditScore.Value >= 750 && application.BehaviourScore is >= 75 && application.MissedPaymentCount == 0)
        {
            baseLimit *= 1.25m;
        }

        return Math.Round(Math.Max(500m, Math.Min(baseLimit, application.LoanAmount)), 2);
    }

    private static LoanApplicationDto MapApplication(LoanApplication application, bool includeBankOnlyFields) =>
        new(
            application.Id,
            application.CustomerId,
            application.LoanProductId,
            application.LoanProduct!.Name,
            application.LoanProduct.InterestRate,
            application.ApplicantFullName,
            application.NationalIdNumber,
            application.PhoneNumber,
            application.Email,
            application.LoanPurpose,
            application.EmploymentStatus,
            application.EmployerOrBusinessName,
            application.EmployerOrBusinessRegistrationNumber,
            application.Status,
            application.LoanAmount,
            application.LoanTermMonths,
            application.MonthlyIncome,
            application.MonthlyExpenses,
            application.ExistingMonthlyDebt,
            application.HasCreditHistoryConsent,
            application.HasIncomeVerificationConsent,
            application.HasPersonalDataProcessingConsent,
            includeBankOnlyFields ? application.CreditScore : null,
            includeBankOnlyFields ? application.CreditScoreSource : null,
            includeBankOnlyFields ? application.CreditScoreCheckedAtUtc : null,
            includeBankOnlyFields ? application.CcrisRecordSummary : null,
            includeBankOnlyFields ? application.CtosScore : null,
            includeBankOnlyFields ? application.InternalAccountHistoryScore : null,
            includeBankOnlyFields ? application.BehaviourScore : null,
            includeBankOnlyFields ? application.FraudRiskScore : null,
            includeBankOnlyFields ? application.KycRiskScore : null,
            includeBankOnlyFields ? application.IncomeVerificationStatus : null,
            includeBankOnlyFields ? application.MissedPaymentCount : 0,
            includeBankOnlyFields ? application.RecommendedInitialLimit : null,
            includeBankOnlyFields ? application.ApprovedLimit : null,
            includeBankOnlyFields && application.IsLimitLocked,
            includeBankOnlyFields ? application.LimitDecisionReason : null,
            includeBankOnlyFields ? application.LimitReviewedAtUtc : null,
            application.OfferedAmount,
            application.OfferedTermMonths,
            application.DecisionNote,
            application.DecisionedAtUtc,
            application.OfferAcceptedAtUtc,
            application.EmploymentDurationMonths,
            application.NumberOfDependents,
            application.ResidentialStatus,
            application.CreatedAtUtc,
            application.SubmittedAtUtc);

    private static AffordabilityAssessmentDto MapAffordability(AffordabilityAssessment assessment) =>
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

    private static RiskAssessmentDto MapRisk(RiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.LoanApplicationId,
            assessment.Score,
            assessment.Grade,
            assessment.Recommendation,
            string.IsNullOrWhiteSpace(assessment.Factors)
                ? []
                : JsonSerializer.Deserialize<IReadOnlyCollection<string>>(assessment.Factors) ?? [],
            assessment.AssessedAtUtc);
}

internal sealed record MockBankCheckResult(
    int CreditScore,
    string CreditScoreSource,
    int CtosScore,
    string CcrisRecordSummary,
    int InternalAccountHistoryScore,
    int BehaviourScore,
    int FraudRiskScore,
    int KycRiskScore,
    string IncomeVerificationStatus,
    int MissedPaymentCount,
    bool ShouldLockOrReduceLimit,
    string? LimitDecisionReason,
    IReadOnlyCollection<string> ProviderNotes);

internal static class MockBankCheckProvider
{
    public static MockBankCheckResult Generate(LoanApplication application)
    {
        var riskSeed = ComputeStableSeed(
            application.NationalIdNumber,
            application.MonthlyIncome,
            application.ExistingMonthlyDebt,
            application.LoanAmount,
            application.EmploymentDurationMonths,
            application.NumberOfDependents);

        var debtPressure = application.MonthlyIncome <= 0
            ? 100m
            : application.ExistingMonthlyDebt / application.MonthlyIncome * 100m;

        var creditScore = Clamp(720 + StableRange(riskSeed, -45, 55) - PressurePenalty(debtPressure), 300, 850);
        var ctosScore = Clamp(690 + StableRange(riskSeed / 3, -35, 65) - PressurePenalty(debtPressure / 2m), 300, 850);
        var internalScore = Clamp(75 + StableRange(riskSeed / 5, -15, 20) + DurationBonus(application.EmploymentDurationMonths), 0, 100);
        var behaviourScore = Clamp(70 + StableRange(riskSeed / 7, -15, 20) - DependentPenalty(application.NumberOfDependents), 0, 100);
        var fraudRiskScore = Clamp(15 + StableRange(riskSeed / 11, -6, 12), 0, 100);
        var kycRiskScore = Clamp(10 + StableRange(riskSeed / 13, -4, 10), 0, 100);
        var missedPayments = creditScore < 620 || ctosScore < 600 ? 1 : 0;
        var incomeVerificationStatus = BuildIncomeVerificationStatus(application, debtPressure);
        var ccrisSummary = BuildCcrisSummary(application, missedPayments, debtPressure);
        var shouldLockLimit = missedPayments > 0 || fraudRiskScore > 50 || kycRiskScore > 50 || creditScore < 620;

        return new MockBankCheckResult(
            creditScore,
            "Mock bureau check",
            ctosScore,
            ccrisSummary,
            internalScore,
            behaviourScore,
            fraudRiskScore,
            kycRiskScore,
            incomeVerificationStatus,
            missedPayments,
            shouldLockLimit,
            shouldLockLimit ? "Automated checks recommend a conservative starting limit." : null,
            [
                "Mock CTOS/credit bureau provider used for demo and development.",
                "CCRIS summary is simulated from declared income, existing debt, and application profile.",
                "Replace this provider with a real CTOS/CCRIS integration before production."
            ]);
    }

    private static int StableRange(int seed, int minInclusive, int maxInclusive)
    {
        var span = maxInclusive - minInclusive + 1;
        return minInclusive + Math.Abs(seed % span);
    }

    private static int ComputeStableSeed(params object?[] values)
    {
        unchecked
        {
            var hash = 17;
            foreach (var value in values)
            {
                foreach (var character in Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }
            }

            return hash == int.MinValue ? 0 : Math.Abs(hash);
        }
    }

    private static int PressurePenalty(decimal debtPressure) =>
        debtPressure switch
        {
            > 60m => 90,
            > 45m => 55,
            > 30m => 25,
            _ => 0
        };

    private static int DurationBonus(int employmentDurationMonths) =>
        employmentDurationMonths switch
        {
            >= 36 => 8,
            >= 12 => 4,
            < 6 => -8,
            _ => 0
        };

    private static int DependentPenalty(int numberOfDependents) =>
        numberOfDependents switch
        {
            >= 5 => 12,
            >= 3 => 6,
            _ => 0
        };

    private static string BuildIncomeVerificationStatus(LoanApplication application, decimal debtPressure)
    {
        if (application.MonthlyIncome < 1800m || debtPressure > 65m)
        {
            return "Mismatch - manual verification required";
        }

        if (application.EmploymentDurationMonths < 6)
        {
            return "Pending - short employment history";
        }

        return "Verified";
    }

    private static string BuildCcrisSummary(LoanApplication application, int missedPayments, decimal debtPressure)
    {
        var activeFacilities = application.ExistingMonthlyDebt > 0 ? 1 : 0;
        var utilisation = debtPressure switch
        {
            > 60m => "high utilisation",
            > 35m => "moderate utilisation",
            _ => "low utilisation"
        };
        var arrears = missedPayments == 0 ? "No arrears" : $"{missedPayments} recent arrears indicator";

        return $"{arrears}, {activeFacilities} active facility, {utilisation}";
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
}
