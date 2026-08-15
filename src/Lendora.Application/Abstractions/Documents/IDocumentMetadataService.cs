using Lendora.Application.Documents;
using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Documents;

public interface IDocumentMetadataService
{
    Task<ServiceResult<ApplicationDocumentDto>> AddMetadataAsync(Guid userId, Guid applicationId, CreateDocumentMetadataRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IReadOnlyCollection<ApplicationDocumentDto>>> GetDocumentsAsync(Guid userId, IReadOnlyCollection<string> roles, Guid applicationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<ApplicationDocumentDto>> GetDocumentAsync(Guid userId, IReadOnlyCollection<string> roles, Guid documentId, CancellationToken cancellationToken = default);
    Task<ServiceResult<ApplicationDocumentDto>> ReviewAsync(Guid reviewerId, Guid documentId, ReviewDocumentRequest request, CancellationToken cancellationToken = default);
}
