using Lendora.Application.Affordability;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Affordability;

public interface IAffordabilityAssessmentService
{
    Task<ServiceResult<AffordabilityAssessmentDto>> GetAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AffordabilityAssessmentDto>> GenerateAssessmentAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);
}
