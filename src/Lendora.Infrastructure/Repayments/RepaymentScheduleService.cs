using Lendora.Application.Abstractions.Repayments;
using Lendora.Application.Loans;
using Lendora.Application.Repayments;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Repayments;

internal sealed class RepaymentScheduleService(ApplicationDbContext dbContext) : IRepaymentScheduleService
{
    public async Task<ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>> GetScheduleAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .AsNoTracking()
            .Include(candidate => candidate.RepaymentScheduleItems)
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>.Failure("Loan application not found.");
        }

        var isStaff = roles.Contains(ApplicationRoles.Admin) ||
            roles.Contains(ApplicationRoles.LoanOfficer) ||
            roles.Contains(ApplicationRoles.Underwriter);

        if (!isStaff && application.CustomerId != userId)
        {
            return ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>.Failure("You do not have access to this repayment schedule.");
        }

        if (application.Status is not LoanApplicationStatus.Approved and not LoanApplicationStatus.OfferAccepted)
        {
            return ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>.Failure("Repayment schedule is available after approval.");
        }

        var schedule = application.RepaymentScheduleItems
            .OrderBy(item => item.InstallmentNumber)
            .Select(MapToDto)
            .ToList();

        return ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>.Success(schedule);
    }

    private static RepaymentScheduleItemDto MapToDto(RepaymentScheduleItem item) =>
        new(
            item.Id,
            item.LoanApplicationId,
            item.InstallmentNumber,
            item.DueDate,
            item.OpeningBalance,
            item.ScheduledPayment,
            item.PrincipalAmount,
            item.InterestAmount,
            item.ClosingBalance);
}
