namespace Lendora.Application.Loans;

public sealed record UpdateBankReviewRequest(
    int? CreditScore,
    string? CreditScoreSource,
    string? CcrisRecordSummary,
    int? CtosScore,
    int? InternalAccountHistoryScore,
    int? BehaviourScore,
    int? FraudRiskScore,
    int? KycRiskScore,
    string? IncomeVerificationStatus,
    int MissedPaymentCount,
    decimal? ApprovedLimit,
    bool IsLimitLocked,
    string? LimitDecisionReason);
