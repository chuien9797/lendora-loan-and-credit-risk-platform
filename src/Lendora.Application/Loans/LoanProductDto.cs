using Lendora.Domain.Enums;

namespace Lendora.Application.Loans;

public sealed record LoanProductDto(
    Guid Id,
    string Code,
    string Name,
    LoanProductType ProductType,
    decimal MinAmount,
    decimal MaxAmount,
    int MinTermMonths,
    int MaxTermMonths,
    decimal InterestRate,
    bool IsActive);
