using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Audit;
using Lendora.Application.Loans;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Audit;

internal sealed class ApplicationAuditService(ApplicationDbContext dbContext) : IApplicationAuditService
{
    public Task RecordAsync(
        Guid loanApplicationId,
        Guid? actorUserId,
        string actorRole,
        string action,
        string summary,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.ApplicationAuditLogs.Add(new ApplicationAuditLog
        {
            LoanApplicationId = loanApplicationId,
            ActorUserId = actorUserId,
            ActorRole = actorRole.Trim(),
            Action = action.Trim(),
            Summary = summary.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim()
        });

        return Task.CompletedTask;
    }

    public async Task<ServiceResult<IReadOnlyCollection<ApplicationAuditLogDto>>> GetForApplicationAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsStaff(roles))
        {
            return ServiceResult<IReadOnlyCollection<ApplicationAuditLogDto>>.Failure("Only staff can view the application audit trail.");
        }

        var applicationExists = await dbContext.LoanApplications
            .AsNoTracking()
            .AnyAsync(application => application.Id == loanApplicationId, cancellationToken);

        if (!applicationExists)
        {
            return ServiceResult<IReadOnlyCollection<ApplicationAuditLogDto>>.Failure("Loan application not found.");
        }

        var logs = await dbContext.ApplicationAuditLogs
            .AsNoTracking()
            .Where(log => log.LoanApplicationId == loanApplicationId)
            .OrderByDescending(log => log.CreatedAtUtc)
            .Select(log => new ApplicationAuditLogDto(
                log.Id,
                log.LoanApplicationId,
                log.ActorUserId,
                log.ActorRole,
                log.Action,
                log.Summary,
                log.Details,
                log.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<ApplicationAuditLogDto>>.Success(logs);
    }

    private static bool IsStaff(IReadOnlyCollection<string> roles)
    {
        return roles.Contains(ApplicationRoles.Admin) ||
            roles.Contains(ApplicationRoles.LoanOfficer) ||
            roles.Contains(ApplicationRoles.Underwriter);
    }
}
