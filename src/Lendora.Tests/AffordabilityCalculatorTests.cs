using Lendora.Application.Affordability;
using Lendora.Domain.Enums;

namespace Lendora.Tests;

public sealed class AffordabilityCalculatorTests
{
    [Fact]
    public void Calculate_UsesStraightLineRepayment_WhenInterestRateIsZero()
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            LoanAmount: 12000m,
            LoanTermMonths: 12,
            AnnualInterestRate: 0m,
            MonthlyIncome: 5000m,
            MonthlyExpenses: 1500m,
            ExistingMonthlyDebt: 250m));

        Assert.Equal(1000m, result.MonthlyRepayment);
        Assert.Equal(12000m, result.TotalRepayment);
        Assert.Equal(0m, result.TotalInterest);
        Assert.Equal(25m, result.DebtServiceRatio);
        Assert.Equal(2250m, result.DisposableIncome);
        Assert.Equal(AffordabilityAssessmentResult.Pass, result.Result);
    }

    [Fact]
    public void Calculate_UsesDecimalAnnualInterestRate_ForMalaysiaProductRates()
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            LoanAmount: 10000m,
            LoanTermMonths: 24,
            AnnualInterestRate: 0.0799m,
            MonthlyIncome: 5000m,
            MonthlyExpenses: 1800m,
            ExistingMonthlyDebt: 500m));

        Assert.Equal(452.23m, result.MonthlyRepayment);
        Assert.Equal(10853.52m, result.TotalRepayment);
        Assert.Equal(853.52m, result.TotalInterest);
        Assert.Equal(19.04m, result.DebtServiceRatio);
        Assert.Equal(2247.77m, result.DisposableIncome);
        Assert.Equal(AffordabilityAssessmentResult.Pass, result.Result);
    }

    [Fact]
    public void Calculate_ReturnsCaution_WhenDsrIsElevated()
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            LoanAmount: 6000m,
            LoanTermMonths: 12,
            AnnualInterestRate: 0m,
            MonthlyIncome: 3000m,
            MonthlyExpenses: 800m,
            ExistingMonthlyDebt: 1100m));

        Assert.Equal(53.33m, result.DebtServiceRatio);
        Assert.Equal(AffordabilityAssessmentResult.Caution, result.Result);
    }

    [Fact]
    public void Calculate_ReturnsFail_WhenDisposableIncomeIsNegative()
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            LoanAmount: 12000m,
            LoanTermMonths: 12,
            AnnualInterestRate: 0m,
            MonthlyIncome: 2500m,
            MonthlyExpenses: 1700m,
            ExistingMonthlyDebt: 200m));

        Assert.Equal(-400m, result.DisposableIncome);
        Assert.Equal(AffordabilityAssessmentResult.Fail, result.Result);
    }

    [Fact]
    public void Calculate_ReturnsFail_WhenMonthlyIncomeIsZero()
    {
        var result = AffordabilityCalculator.Calculate(new AffordabilityCalculationInput(
            LoanAmount: 6000m,
            LoanTermMonths: 12,
            AnnualInterestRate: 0m,
            MonthlyIncome: 0m,
            MonthlyExpenses: 1000m,
            ExistingMonthlyDebt: 0m));

        Assert.Equal(999.99m, result.DebtServiceRatio);
        Assert.Equal(-1500m, result.DisposableIncome);
        Assert.Equal(AffordabilityAssessmentResult.Fail, result.Result);
    }
}
