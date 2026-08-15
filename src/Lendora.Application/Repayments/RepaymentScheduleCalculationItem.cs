namespace Lendora.Application.Repayments;

public sealed record RepaymentScheduleCalculationItem(
    int InstallmentNumber,
    DateOnly DueDate,
    decimal OpeningBalance,
    decimal ScheduledPayment,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal ClosingBalance);
