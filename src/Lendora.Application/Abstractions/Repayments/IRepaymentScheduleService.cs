using Lendora.Application.Loans;
using Lendora.Application.Repayments;

namespace Lendora.Application.Abstractions.Repayments;

public interface IRepaymentScheduleService
{
    Task<ServiceResult<IReadOnlyCollection<RepaymentScheduleItemDto>>> GetScheduleAsync(Guid userId, IReadOnlyCollection<string> roles, Guid applicationId, CancellationToken cancellationToken = default);
}
