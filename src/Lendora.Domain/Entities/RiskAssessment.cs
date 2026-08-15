using Lendora.Domain.Enums;

namespace Lendora.Domain.Entities;

public sealed class RiskAssessment : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public int Score { get; set; }
    public RiskAssessmentGrade Grade { get; set; }
    public RiskAssessmentRecommendation Recommendation { get; set; }
    public string Factors { get; set; } = string.Empty;
    public DateTime AssessedAtUtc { get; set; } = DateTime.UtcNow;

    public LoanApplication? LoanApplication { get; set; }
}
