using Lendora.Application.Loans;
using Lendora.Domain.Entities;

namespace Lendora.Infrastructure.Documents;

internal sealed class DisabledDocumentTextExtractor : IDocumentTextExtractor
{
    public Task<ServiceResult<ExtractedDocumentText>> ExtractAsync(
        ApplicationDocument document,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ServiceResult<ExtractedDocumentText>.Failure("Document OCR is not configured for this environment."));
    }
}
