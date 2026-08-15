using Lendora.Application.Documents;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Documents;

public interface IDocumentOcrService
{
    Task<ServiceResult<ApplicationDocumentDto>> ExtractAsync(
        Guid reviewerId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
