using Lendora.Application.Loans;
using Lendora.Application.Risk;

namespace Lendora.Application.Abstractions.Risk;

public interface IRiskAssessmentService
{
    Task<ServiceResult<RiskAssessmentDto>> GetAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<RiskAssessmentDto>> GenerateAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);
}
