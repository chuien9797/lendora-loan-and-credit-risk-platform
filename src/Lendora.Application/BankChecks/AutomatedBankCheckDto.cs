using Lendora.Application.Affordability;
using Lendora.Application.Loans;
using Lendora.Application.Risk;

namespace Lendora.Application.BankChecks;

public sealed record AutomatedBankCheckDto(
    LoanApplicationDto Application,
    AffordabilityAssessmentDto Affordability,
    RiskAssessmentDto Risk,
    IReadOnlyCollection<string> ProviderNotes);
