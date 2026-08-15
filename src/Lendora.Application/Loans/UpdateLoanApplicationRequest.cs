using Lendora.Domain.Enums;

namespace Lendora.Application.Loans;

public sealed record UpdateLoanApplicationRequest(
    Guid LoanProductId,
    string ApplicantFullName,
    string NationalIdNumber,
    string PhoneNumber,
    string Email,
    string LoanPurpose,
    EmploymentStatus EmploymentStatus,
    string EmployerOrBusinessName,
    string? EmployerOrBusinessRegistrationNumber,
    decimal LoanAmount,
    int LoanTermMonths,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal ExistingMonthlyDebt,
    bool HasCreditHistoryConsent,
    bool HasIncomeVerificationConsent,
    bool HasPersonalDataProcessingConsent,
    int EmploymentDurationMonths,
    int NumberOfDependents,
    ResidentialStatus ResidentialStatus);
