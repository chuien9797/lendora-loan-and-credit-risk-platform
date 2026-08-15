using Lendora.Domain.Enums;

namespace Lendora.Application.Loans;

public sealed record UpdateApplicationDecisionRequest(
    LoanApplicationStatus Status,
    decimal? OfferedAmount,
    int? OfferedTermMonths,
    string? DecisionNote);
