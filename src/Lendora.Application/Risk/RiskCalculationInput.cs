using Lendora.Domain.Enums;

namespace Lendora.Application.Risk;

public sealed record RiskCalculationInput(
    int? CreditScore,
    int? CtosScore,
    int? InternalAccountHistoryScore,
    int? BehaviourScore,
    int? FraudRiskScore,
    int? KycRiskScore,
    string? IncomeVerificationStatus,
    int MissedPaymentCount,
    int EmploymentDurationMonths,
    int NumberOfDependents,
    EmploymentStatus EmploymentStatus,
    ResidentialStatus ResidentialStatus,
    decimal DebtServiceRatio,
    decimal DisposableIncome,
    AffordabilityAssessmentResult AffordabilityResult);
