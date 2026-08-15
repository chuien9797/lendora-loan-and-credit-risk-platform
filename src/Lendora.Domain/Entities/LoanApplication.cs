using Lendora.Domain.Enums;

namespace Lendora.Domain.Entities;

public class LoanApplication : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid LoanProductId { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public string ApplicantFullName { get; set; } = string.Empty;
    public string NationalIdNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; }
    public string EmployerOrBusinessName { get; set; } = string.Empty;
    public string? EmployerOrBusinessRegistrationNumber { get; set; }
    public LoanApplicationStatus Status { get; set; } = LoanApplicationStatus.Draft;
    public decimal LoanAmount { get; set; }
    public int LoanTermMonths { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal ExistingMonthlyDebt { get; set; }
    public bool HasCreditHistoryConsent { get; set; }
    public bool HasIncomeVerificationConsent { get; set; }
    public bool HasPersonalDataProcessingConsent { get; set; }
    public int? CreditScore { get; set; }
    public string? CreditScoreSource { get; set; }
    public DateTime? CreditScoreCheckedAtUtc { get; set; }
    public string? CcrisRecordSummary { get; set; }
    public int? CtosScore { get; set; }
    public int? InternalAccountHistoryScore { get; set; }
    public int? BehaviourScore { get; set; }
    public int? FraudRiskScore { get; set; }
    public int? KycRiskScore { get; set; }
    public string? IncomeVerificationStatus { get; set; }
    public int MissedPaymentCount { get; set; }
    public decimal? RecommendedInitialLimit { get; set; }
    public decimal? ApprovedLimit { get; set; }
    public bool IsLimitLocked { get; set; }
    public string? LimitDecisionReason { get; set; }
    public DateTime? LimitReviewedAtUtc { get; set; }
    public Guid? LimitReviewedByUserId { get; set; }
    public decimal? OfferedAmount { get; set; }
    public int? OfferedTermMonths { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime? DecisionedAtUtc { get; set; }
    public Guid? DecisionedByUserId { get; set; }
    public DateTime? OfferAcceptedAtUtc { get; set; }
    public int EmploymentDurationMonths { get; set; }
    public int NumberOfDependents { get; set; }
    public ResidentialStatus ResidentialStatus { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public LoanProduct? LoanProduct { get; set; }
    public AffordabilityAssessment? AffordabilityAssessment { get; set; }
    public RiskAssessment? RiskAssessment { get; set; }
    public ICollection<ApplicationDocument> Documents { get; set; } = [];
    public ICollection<RepaymentScheduleItem> RepaymentScheduleItems { get; set; } = [];
    public ICollection<ApplicationAuditLog> AuditLogs { get; set; } = [];
}
