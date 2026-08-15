using Lendora.Domain.Enums;

namespace Lendora.Application.Affordability;

public sealed record AffordabilityAssessmentDto(
    Guid Id,
    Guid LoanApplicationId,
    decimal MonthlyRepayment,
    decimal TotalRepayment,
    decimal TotalInterest,
    decimal DebtServiceRatio,
    decimal DisposableIncome,
    AffordabilityAssessmentResult Result,
    DateTime AssessedAtUtc);
