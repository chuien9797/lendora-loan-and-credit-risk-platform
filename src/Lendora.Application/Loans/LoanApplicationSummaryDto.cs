using Lendora.Domain.Enums;

namespace Lendora.Application.Loans;

public sealed record LoanApplicationSummaryDto(
    Guid Id,
    Guid CustomerId,
    Guid LoanProductId,
    string LoanProductName,
    string ApplicantFullName,
    string NationalIdNumber,
    string PhoneNumber,
    string Email,
    string LoanPurpose,
    LoanApplicationStatus Status,
    decimal LoanAmount,
    int LoanTermMonths,
    decimal LoanProductInterestRate,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc);
