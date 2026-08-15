using Lendora.Application.BankChecks;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.BankChecks;

public interface IAutomatedBankCheckService
{
    Task<ServiceResult<AutomatedBankCheckDto>> RunAsync(Guid applicationId, Guid? reviewedByUserId, CancellationToken cancellationToken = default);
}
