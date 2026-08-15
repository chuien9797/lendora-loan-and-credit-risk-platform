using Lendora.Domain.Enums;

namespace Lendora.Application.Admin;

public sealed record CreateLoanProductRequest(
    string Code,
    string Name,
    LoanProductType ProductType,
    decimal MinAmount,
    decimal MaxAmount,
    int MinTermMonths,
    int MaxTermMonths,
    decimal InterestRate,
    bool IsActive = true);
