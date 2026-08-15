using Lendora.Application.Loans;

namespace Lendora.Application.Abstractions.Documents;

public interface IDocumentStorageService
{
    Task<ServiceResult<StoredDocumentFile>> SaveAsync(
        Guid applicationId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<ServiceResult<StoredDocumentContent>> OpenReadAsync(
        string storagePath,
        string contentType,
        string downloadFileName,
        CancellationToken cancellationToken = default);
}

public sealed record StoredDocumentFile(
    string StoredFileName,
    string StoragePath);

public sealed record StoredDocumentContent(
    Stream Content,
    string ContentType,
    string DownloadFileName);
