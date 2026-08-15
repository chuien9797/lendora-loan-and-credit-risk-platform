using Lendora.Application.Audit;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Audit;

public interface IApplicationAuditService
{
    Task RecordAsync(
        Guid loanApplicationId,
        Guid? actorUserId,
        string actorRole,
        string action,
        string summary,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyCollection<ApplicationAuditLogDto>>> GetForApplicationAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);
}
