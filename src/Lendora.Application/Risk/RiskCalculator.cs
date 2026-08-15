using Lendora.Domain.Enums;

namespace Lendora.Application.Risk;

public static class RiskCalculator
{
    public static RiskCalculationResult Calculate(RiskCalculationInput input)
    {
        var score = 100;
        var factors = new List<string>();

        ApplyCreditScore(input.CreditScore, ref score, factors);
        ApplyCtosScore(input.CtosScore, ref score, factors);
        ApplyBehaviourScore(input.BehaviourScore, input.InternalAccountHistoryScore, input.MissedPaymentCount, ref score, factors);
        ApplyVerificationRisk(input.IncomeVerificationStatus, input.FraudRiskScore, input.KycRiskScore, ref score, factors);
        ApplyDebtServiceRatio(input.DebtServiceRatio, ref score, factors);
        ApplyDisposableIncome(input.DisposableIncome, ref score, factors);
        ApplyAffordabilityResult(input.AffordabilityResult, ref score, factors);
        ApplyEmploymentStability(input.EmploymentStatus, input.EmploymentDurationMonths, ref score, factors);
        ApplyResidentialStability(input.ResidentialStatus, ref score, factors);
        ApplyDependents(input.NumberOfDependents, ref score, factors);

        score = Math.Clamp(score, 0, 100);
        var grade = ClassifyGrade(score);
        var recommendation = Recommend(score, input.AffordabilityResult, input.CreditScore);

        return new RiskCalculationResult(score, grade, recommendation, factors);
    }

    private static void ApplyCreditScore(int? creditScore, ref int score, ICollection<string> factors)
    {
        if (!creditScore.HasValue)
        {
            score -= 20;
            factors.Add("Credit bureau score is not available; manual review is required.");
            return;
        }

        switch (creditScore.Value)
        {
            case >= 750:
                factors.Add("Strong credit profile.");
                break;
            case >= 680:
                score -= 8;
                factors.Add("Good credit profile with moderate risk.");
                break;
            case >= 620:
                score -= 18;
                factors.Add("Fair credit profile increases repayment risk.");
                break;
            case >= 560:
                score -= 30;
                factors.Add("Weak credit profile requires manual review.");
                break;
            default:
                score -= 45;
                factors.Add("Very weak credit profile creates high default risk.");
                break;
        }
    }

    private static void ApplyCtosScore(int? ctosScore, ref int score, ICollection<string> factors)
    {
        if (!ctosScore.HasValue)
        {
            factors.Add("CTOS score is not recorded separately.");
            return;
        }

        switch (ctosScore.Value)
        {
            case >= 750:
                factors.Add("CTOS score supports a strong credit profile.");
                break;
            case >= 650:
                score -= 6;
                factors.Add("CTOS score is acceptable with some repayment risk.");
                break;
            case >= 550:
                score -= 14;
                factors.Add("CTOS score suggests elevated credit risk.");
                break;
            default:
                score -= 26;
                factors.Add("CTOS score is weak and supports manual review or lower limit.");
                break;
        }
    }

    private static void ApplyBehaviourScore(int? behaviourScore, int? internalAccountHistoryScore, int missedPaymentCount, ref int score, ICollection<string> factors)
    {
        if (!behaviourScore.HasValue)
        {
            score -= 6;
            factors.Add("Behaviour score is not available yet, so the starting limit should stay conservative.");
        }
        else
        {
            switch (behaviourScore.Value)
            {
                case >= 80:
                    factors.Add("Internal behaviour score supports repayment reliability.");
                    break;
                case >= 60:
                    score -= 6;
                    factors.Add("Internal behaviour score is acceptable but should be monitored.");
                    break;
                case >= 40:
                    score -= 14;
                    factors.Add("Internal behaviour score suggests elevated repayment risk.");
                    break;
                default:
                    score -= 25;
                    factors.Add("Internal behaviour score is weak and supports limit reduction or lock.");
                    break;
            }
        }

        if (internalAccountHistoryScore.HasValue)
        {
            switch (internalAccountHistoryScore.Value)
            {
                case >= 80:
                    factors.Add("Internal bank account history supports stable repayment behaviour.");
                    break;
                case >= 60:
                    score -= 4;
                    factors.Add("Internal bank account history is acceptable but not strong.");
                    break;
                case >= 40:
                    score -= 10;
                    factors.Add("Internal bank account history suggests account conduct risk.");
                    break;
                default:
                    score -= 18;
                    factors.Add("Internal bank account history is weak.");
                    break;
            }
        }

        switch (missedPaymentCount)
        {
            case <= 0:
                factors.Add("No missed payment behaviour has been recorded.");
                break;
            case <= 1:
                score -= 10;
                factors.Add("One missed payment has been recorded; limit should not increase.");
                break;
            default:
                score -= 24;
                factors.Add("Multiple missed payments support reducing or locking the limit.");
                break;
        }
    }

    private static void ApplyVerificationRisk(string? incomeVerificationStatus, int? fraudRiskScore, int? kycRiskScore, ref int score, ICollection<string> factors)
    {
        if (!string.IsNullOrWhiteSpace(incomeVerificationStatus))
        {
            var normalizedStatus = incomeVerificationStatus.Trim().ToLowerInvariant();
            if (normalizedStatus.Contains("verified"))
            {
                factors.Add("Income documents are verified.");
            }
            else if (normalizedStatus.Contains("pending"))
            {
                score -= 8;
                factors.Add("Income verification is pending.");
            }
            else if (normalizedStatus.Contains("failed") || normalizedStatus.Contains("mismatch"))
            {
                score -= 25;
                factors.Add("Income verification failed or has mismatches.");
            }
        }
        else
        {
            score -= 6;
            factors.Add("Income verification status is not recorded.");
        }

        ApplyRiskScore("Fraud", fraudRiskScore, ref score, factors);
        ApplyRiskScore("KYC", kycRiskScore, ref score, factors);
    }

    private static void ApplyRiskScore(string label, int? riskScore, ref int score, ICollection<string> factors)
    {
        if (!riskScore.HasValue)
        {
            factors.Add($"{label} risk score is not recorded.");
            return;
        }

        switch (riskScore.Value)
        {
            case <= 20:
                factors.Add($"{label} risk is low.");
                break;
            case <= 50:
                score -= 8;
                factors.Add($"{label} risk is moderate.");
                break;
            default:
                score -= 22;
                factors.Add($"{label} risk is high.");
                break;
        }
    }

    private static void ApplyDebtServiceRatio(decimal debtServiceRatio, ref int score, ICollection<string> factors)
    {
        switch (debtServiceRatio)
        {
            case <= 35m:
                factors.Add("DSR is healthy.");
                break;
            case <= 50m:
                score -= 10;
                factors.Add("DSR is elevated.");
                break;
            case <= 60m:
                score -= 22;
                factors.Add("DSR is high.");
                break;
            default:
                score -= 35;
                factors.Add("DSR is above policy tolerance.");
                break;
        }
    }

    private static void ApplyDisposableIncome(decimal disposableIncome, ref int score, ICollection<string> factors)
    {
        switch (disposableIncome)
        {
            case >= 1500m:
                factors.Add("Disposable income buffer is strong.");
                break;
            case >= 500m:
                score -= 8;
                factors.Add("Disposable income buffer is acceptable but limited.");
                break;
            case >= 0m:
                score -= 20;
                factors.Add("Disposable income buffer is thin.");
                break;
            default:
                score -= 40;
                factors.Add("Disposable income is negative after repayment.");
                break;
        }
    }

    private static void ApplyAffordabilityResult(AffordabilityAssessmentResult affordabilityResult, ref int score, ICollection<string> factors)
    {
        switch (affordabilityResult)
        {
            case AffordabilityAssessmentResult.Pass:
                factors.Add("Affordability assessment passed.");
                break;
            case AffordabilityAssessmentResult.Caution:
                score -= 15;
                factors.Add("Affordability assessment returned caution.");
                break;
            case AffordabilityAssessmentResult.Fail:
                score -= 35;
                factors.Add("Affordability assessment failed.");
                break;
        }
    }

    private static void ApplyEmploymentStability(EmploymentStatus employmentStatus, int employmentDurationMonths, ref int score, ICollection<string> factors)
    {
        if (employmentStatus is EmploymentStatus.Unemployed or EmploymentStatus.Student)
        {
            score -= 25;
            factors.Add("Employment status indicates unstable income.");
            return;
        }

        if (employmentStatus == EmploymentStatus.SelfEmployed)
        {
            score -= 8;
            factors.Add("Self-employed income may need additional verification.");
        }

        switch (employmentDurationMonths)
        {
            case >= 24:
                factors.Add("Employment duration supports income stability.");
                break;
            case >= 6:
                score -= 8;
                factors.Add("Employment duration is moderate.");
                break;
            default:
                score -= 16;
                factors.Add("Short employment duration increases income stability risk.");
                break;
        }
    }

    private static void ApplyResidentialStability(ResidentialStatus residentialStatus, ref int score, ICollection<string> factors)
    {
        switch (residentialStatus)
        {
            case ResidentialStatus.Owner:
            case ResidentialStatus.Mortgage:
                factors.Add("Residential status supports stability.");
                break;
            case ResidentialStatus.Tenant:
            case ResidentialStatus.LivingWithFamily:
                score -= 5;
                factors.Add("Residential status has moderate stability.");
                break;
            default:
                score -= 10;
                factors.Add("Residential status requires additional review.");
                break;
        }
    }

    private static void ApplyDependents(int numberOfDependents, ref int score, ICollection<string> factors)
    {
        switch (numberOfDependents)
        {
            case <= 2:
                factors.Add("Dependent count is within normal tolerance.");
                break;
            case <= 4:
                score -= 5;
                factors.Add("Dependent count modestly increases household pressure.");
                break;
            default:
                score -= 10;
                factors.Add("High dependent count increases household pressure.");
                break;
        }
    }

    private static RiskAssessmentGrade ClassifyGrade(int score)
    {
        return score switch
        {
            >= 75 => RiskAssessmentGrade.Low,
            >= 55 => RiskAssessmentGrade.Medium,
            _ => RiskAssessmentGrade.High
        };
    }

    private static RiskAssessmentRecommendation Recommend(int score, AffordabilityAssessmentResult affordabilityResult, int? creditScore)
    {
        if (affordabilityResult == AffordabilityAssessmentResult.Fail || score < 45)
        {
            return RiskAssessmentRecommendation.Decline;
        }

        return score >= 75 && affordabilityResult == AffordabilityAssessmentResult.Pass && creditScore.HasValue
            ? RiskAssessmentRecommendation.AutoApprove
            : RiskAssessmentRecommendation.ManualReview;
    }
}
