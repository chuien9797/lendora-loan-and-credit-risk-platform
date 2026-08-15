using Lendora.Application.Repayments;

namespace Lendora.Tests;

public sealed class RepaymentScheduleCalculatorTests
{
    [Fact]
    public void Calculate_UsesEqualPrincipalPayments_WhenInterestRateIsZero()
    {
        var schedule = RepaymentScheduleCalculator.Calculate(new RepaymentScheduleCalculationInput(
            12000m,
            12,
            0m,
            new DateOnly(2026, 9, 1))).ToList();

        Assert.Equal(12, schedule.Count);
        Assert.All(schedule, item => Assert.Equal(1000m, item.ScheduledPayment));
        Assert.Equal(0m, schedule[^1].ClosingBalance);
    }

    [Fact]
    public void Calculate_AdjustsFinalInstallment_ToClearRoundingBalance()
    {
        var schedule = RepaymentScheduleCalculator.Calculate(new RepaymentScheduleCalculationInput(
            10000m,
            24,
            8.5m,
            new DateOnly(2026, 9, 1))).ToList();

        Assert.Equal(24, schedule.Count);
        Assert.Equal(454.56m, schedule[0].ScheduledPayment);
        Assert.Equal(0m, schedule[^1].ClosingBalance);
        Assert.Equal(new DateOnly(2028, 8, 1), schedule[^1].DueDate);
    }
}
