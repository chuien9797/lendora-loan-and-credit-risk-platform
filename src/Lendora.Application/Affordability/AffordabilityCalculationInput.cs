namespace Lendora.Application.Affordability;

public sealed record AffordabilityCalculationInput(
    decimal LoanAmount,
    int LoanTermMonths,
    decimal AnnualInterestRate,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal ExistingMonthlyDebt);
