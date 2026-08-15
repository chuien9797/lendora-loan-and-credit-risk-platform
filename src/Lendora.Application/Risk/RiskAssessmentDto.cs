using Lendora.Domain.Enums;

namespace Lendora.Application.Risk;

public sealed record RiskAssessmentDto(
    Guid Id,
    Guid LoanApplicationId,
    int Score,
    RiskAssessmentGrade Grade,
    RiskAssessmentRecommendation Recommendation,
    IReadOnlyCollection<string> Factors,
    DateTime AssessedAtUtc);
