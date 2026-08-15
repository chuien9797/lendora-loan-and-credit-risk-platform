using Lendora.Application.Risk;
using Lendora.Domain.Enums;

namespace Lendora.Tests;

public sealed class RiskCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsAutoApprove_ForStrongLowRiskProfile()
    {
        var result = RiskCalculator.Calculate(new RiskCalculationInput(
            CreditScore: 790,
            CtosScore: 800,
            InternalAccountHistoryScore: 90,
            BehaviourScore: 90,
            FraudRiskScore: 10,
            KycRiskScore: 10,
            IncomeVerificationStatus: "Verified",
            MissedPaymentCount: 0,
            EmploymentDurationMonths: 36,
            NumberOfDependents: 1,
            EmploymentStatus: EmploymentStatus.Employed,
            ResidentialStatus: ResidentialStatus.Owner,
            DebtServiceRatio: 24m,
            DisposableIncome: 2500m,
            AffordabilityResult: AffordabilityAssessmentResult.Pass));

        Assert.Equal(100, result.Score);
        Assert.Equal(RiskAssessmentGrade.Low, result.Grade);
        Assert.Equal(RiskAssessmentRecommendation.AutoApprove, result.Recommendation);
    }

    [Fact]
    public void Calculate_ReturnsManualReview_ForMediumRiskProfile()
    {
        var result = RiskCalculator.Calculate(new RiskCalculationInput(
            CreditScore: 700,
            CtosScore: null,
            InternalAccountHistoryScore: null,
            BehaviourScore: 80,
            FraudRiskScore: 10,
            KycRiskScore: 10,
            IncomeVerificationStatus: "Verified",
            MissedPaymentCount: 0,
            EmploymentDurationMonths: 18,
            NumberOfDependents: 2,
            EmploymentStatus: EmploymentStatus.SelfEmployed,
            ResidentialStatus: ResidentialStatus.Tenant,
            DebtServiceRatio: 45m,
            DisposableIncome: 900m,
            AffordabilityResult: AffordabilityAssessmentResult.Pass));

        Assert.Equal(53, result.Score);
        Assert.Equal(RiskAssessmentGrade.High, result.Grade);
        Assert.Equal(RiskAssessmentRecommendation.ManualReview, result.Recommendation);
    }

    [Fact]
    public void Calculate_ReturnsDecline_WhenAffordabilityFails()
    {
        var result = RiskCalculator.Calculate(new RiskCalculationInput(
            CreditScore: 720,
            CtosScore: 710,
            InternalAccountHistoryScore: 75,
            BehaviourScore: 70,
            FraudRiskScore: 10,
            KycRiskScore: 10,
            IncomeVerificationStatus: "Verified",
            MissedPaymentCount: 0,
            EmploymentDurationMonths: 30,
            NumberOfDependents: 1,
            EmploymentStatus: EmploymentStatus.Employed,
            ResidentialStatus: ResidentialStatus.Mortgage,
            DebtServiceRatio: 62m,
            DisposableIncome: -100m,
            AffordabilityResult: AffordabilityAssessmentResult.Fail));

        Assert.Equal(RiskAssessmentGrade.High, result.Grade);
        Assert.Equal(RiskAssessmentRecommendation.Decline, result.Recommendation);
    }

    [Fact]
    public void Calculate_ReturnsManualReview_WhenCreditScoreIsMissing()
    {
        var result = RiskCalculator.Calculate(new RiskCalculationInput(
            CreditScore: null,
            CtosScore: null,
            InternalAccountHistoryScore: null,
            BehaviourScore: 82,
            FraudRiskScore: 10,
            KycRiskScore: 10,
            IncomeVerificationStatus: "Verified",
            MissedPaymentCount: 0,
            EmploymentDurationMonths: 36,
            NumberOfDependents: 0,
            EmploymentStatus: EmploymentStatus.Employed,
            ResidentialStatus: ResidentialStatus.Owner,
            DebtServiceRatio: 24m,
            DisposableIncome: 2500m,
            AffordabilityResult: AffordabilityAssessmentResult.Pass));

        Assert.Equal(RiskAssessmentRecommendation.ManualReview, result.Recommendation);
        Assert.Contains(result.Factors, factor => factor.Contains("manual review"));
    }
}
