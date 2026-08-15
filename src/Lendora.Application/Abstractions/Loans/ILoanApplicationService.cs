using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Loans;

public interface ILoanApplicationService
{
    Task<IReadOnlyCollection<LoanProductDto>> GetLoanProductsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> CreateDraftAsync(Guid customerId, CreateLoanApplicationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> UpdateDraftAsync(Guid customerId, Guid applicationId, UpdateLoanApplicationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> SubmitAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> AcceptOfferAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> UpdateBankReviewAsync(Guid staffUserId, Guid applicationId, UpdateBankReviewRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> UpdateDecisionAsync(Guid staffUserId, IReadOnlyCollection<string> roles, Guid applicationId, UpdateApplicationDecisionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> DeleteDraftAsync(Guid customerId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoanApplicationSummaryDto>> GetMyApplicationsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanApplicationDto>> GetApplicationAsync(Guid userId, IReadOnlyCollection<string> roles, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoanApplicationSummaryDto>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoanApplicationSummaryDto>> SearchApplicationsAsync(string query, CancellationToken cancellationToken = default);
}
