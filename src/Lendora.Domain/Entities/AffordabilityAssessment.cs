using Lendora.Domain.Enums;

namespace Lendora.Domain.Entities;

public sealed class AffordabilityAssessment : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public decimal MonthlyRepayment { get; set; }
    public decimal TotalRepayment { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal DebtServiceRatio { get; set; }
    public decimal DisposableIncome { get; set; }
    public AffordabilityAssessmentResult Result { get; set; }
    public DateTime AssessedAtUtc { get; set; } = DateTime.UtcNow;

    public LoanApplication? LoanApplication { get; set; }
}
