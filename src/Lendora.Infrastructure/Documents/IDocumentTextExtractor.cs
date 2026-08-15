using Lendora.Application.Loans;
using Lendora.Domain.Entities;

namespace Lendora.Infrastructure.Documents;

internal interface IDocumentTextExtractor
{
    Task<ServiceResult<ExtractedDocumentText>> ExtractAsync(
        ApplicationDocument document,
        CancellationToken cancellationToken = default);
}
