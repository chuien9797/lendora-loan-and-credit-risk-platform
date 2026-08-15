namespace Lendora.Application.Repayments;

public static class RepaymentScheduleCalculator
{
    public static IReadOnlyCollection<RepaymentScheduleCalculationItem> Calculate(RepaymentScheduleCalculationInput input)
    {
        if (input.Principal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Principal must be greater than zero.");
        }

        if (input.TermMonths <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Loan term must be greater than zero.");
        }

        var monthlyRate = NormalizeAnnualInterestRate(input.AnnualInterestRate) / 12m;
        var regularPayment = CalculateMonthlyPayment(input.Principal, input.TermMonths, monthlyRate);
        var balance = input.Principal;
        var items = new List<RepaymentScheduleCalculationItem>();

        for (var installment = 1; installment <= input.TermMonths; installment++)
        {
            var openingBalance = balance;
            var interest = decimal.Round(openingBalance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var payment = regularPayment;
            var principal = decimal.Round(payment - interest, 2, MidpointRounding.AwayFromZero);

            if (installment == input.TermMonths || principal > openingBalance)
            {
                principal = openingBalance;
                payment = decimal.Round(principal + interest, 2, MidpointRounding.AwayFromZero);
            }

            balance = decimal.Round(openingBalance - principal, 2, MidpointRounding.AwayFromZero);

            items.Add(new RepaymentScheduleCalculationItem(
                installment,
                input.FirstDueDate.AddMonths(installment - 1),
                openingBalance,
                payment,
                principal,
                interest,
                balance));
        }

        return items;
    }

    private static decimal CalculateMonthlyPayment(decimal principal, int termMonths, decimal monthlyRate)
    {
        if (monthlyRate <= 0)
        {
            return decimal.Round(principal / termMonths, 2, MidpointRounding.AwayFromZero);
        }

        var monthlyRateAsDouble = (double)monthlyRate;
        var factor = Math.Pow(1 + monthlyRateAsDouble, termMonths);
        var payment = (double)principal * monthlyRateAsDouble * factor / (factor - 1);

        return decimal.Round((decimal)payment, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeAnnualInterestRate(decimal annualInterestRate)
    {
        return annualInterestRate > 1m
            ? annualInterestRate / 100m
            : annualInterestRate;
    }
}
