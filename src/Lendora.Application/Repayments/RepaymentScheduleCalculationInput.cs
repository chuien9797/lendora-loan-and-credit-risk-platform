namespace Lendora.Application.Repayments;

public sealed record RepaymentScheduleCalculationInput(
    decimal Principal,
    int TermMonths,
    decimal AnnualInterestRate,
    DateOnly FirstDueDate);
