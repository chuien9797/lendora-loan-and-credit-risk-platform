using Lendora.Domain.Enums;

namespace Lendora.Application.Affordability;

public static class AffordabilityCalculator
{
    private const decimal CautionDebtServiceRatioThreshold = 50m;
    private const decimal FailDebtServiceRatioThreshold = 60m;
    private const decimal CautionDisposableIncomeThreshold = 500m;
    private const decimal NoIncomeDebtServiceRatio = 999.99m;

    public static AffordabilityCalculationResult Calculate(AffordabilityCalculationInput input)
    {
        if (input.LoanTermMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Loan term must be greater than zero.");
        }

        if (input.MonthlyIncome < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Monthly income cannot be negative.");
        }

        var monthlyRepayment = CalculateMonthlyRepayment(
            input.LoanAmount,
            input.LoanTermMonths,
            input.AnnualInterestRate);

        var totalRepayment = decimal.Round(monthlyRepayment * input.LoanTermMonths, 2, MidpointRounding.AwayFromZero);
        var totalInterest = decimal.Round(totalRepayment - input.LoanAmount, 2, MidpointRounding.AwayFromZero);
        var debtServiceRatio = input.MonthlyIncome == 0
            ? NoIncomeDebtServiceRatio
            : decimal.Round(
                (input.ExistingMonthlyDebt + monthlyRepayment) / input.MonthlyIncome * 100m,
                2,
                MidpointRounding.AwayFromZero);
        var disposableIncome = decimal.Round(
            input.MonthlyIncome - input.MonthlyExpenses - input.ExistingMonthlyDebt - monthlyRepayment,
            2,
            MidpointRounding.AwayFromZero);

        var result = Classify(debtServiceRatio, disposableIncome);

        return new AffordabilityCalculationResult(
            monthlyRepayment,
            totalRepayment,
            totalInterest,
            debtServiceRatio,
            disposableIncome,
            result);
    }

    private static decimal CalculateMonthlyRepayment(decimal principal, int termMonths, decimal annualInterestRate)
    {
        if (annualInterestRate <= 0)
        {
            return decimal.Round(principal / termMonths, 2, MidpointRounding.AwayFromZero);
        }

        var normalizedAnnualInterestRate = NormalizeAnnualInterestRate(annualInterestRate);
        var monthlyRate = (double)(normalizedAnnualInterestRate / 12m);
        var factor = Math.Pow(1 + monthlyRate, termMonths);
        var repayment = (double)principal * monthlyRate * factor / (factor - 1);

        return decimal.Round((decimal)repayment, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeAnnualInterestRate(decimal annualInterestRate)
    {
        return annualInterestRate > 1m
            ? annualInterestRate / 100m
            : annualInterestRate;
    }

    private static AffordabilityAssessmentResult Classify(decimal debtServiceRatio, decimal disposableIncome)
    {
        if (debtServiceRatio > FailDebtServiceRatioThreshold || disposableIncome < 0)
        {
            return AffordabilityAssessmentResult.Fail;
        }

        if (debtServiceRatio > CautionDebtServiceRatioThreshold || disposableIncome < CautionDisposableIncomeThreshold)
        {
            return AffordabilityAssessmentResult.Caution;
        }

        return AffordabilityAssessmentResult.Pass;
    }
}
