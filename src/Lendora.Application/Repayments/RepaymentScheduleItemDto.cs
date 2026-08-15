namespace Lendora.Application.Repayments;

public sealed record RepaymentScheduleItemDto(
    Guid Id,
    Guid LoanApplicationId,
    int InstallmentNumber,
    DateOnly DueDate,
    decimal OpeningBalance,
    decimal ScheduledPayment,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal ClosingBalance);
