using Lendora.Domain.Enums;

namespace Lendora.Application.Affordability;

public sealed record AffordabilityCalculationResult(
    decimal MonthlyRepayment,
    decimal TotalRepayment,
    decimal TotalInterest,
    decimal DebtServiceRatio,
    decimal DisposableIncome,
    AffordabilityAssessmentResult Result);
