namespace Lendora.Application.Audit;

public sealed record ApplicationAuditLogDto(
    Guid Id,
    Guid LoanApplicationId,
    Guid? ActorUserId,
    string ActorRole,
    string Action,
    string Summary,
    string? Details,
    DateTime CreatedAtUtc);
