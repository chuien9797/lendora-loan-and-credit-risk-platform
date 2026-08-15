using Lendora.Domain.Enums;

namespace Lendora.Application.Risk;

public sealed record RiskCalculationResult(
    int Score,
    RiskAssessmentGrade Grade,
    RiskAssessmentRecommendation Recommendation,
    IReadOnlyCollection<string> Factors);
