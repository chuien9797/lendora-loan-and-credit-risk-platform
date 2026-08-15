using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Loans;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lendora.Infrastructure.Documents;

internal sealed class LocalDocumentStorageService(
    IHostEnvironment environment,
    IOptions<DocumentStorageOptions> options) : IDocumentStorageService
{
    public async Task<ServiceResult<StoredDocumentFile>> SaveAsync(
        Guid applicationId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeOriginalFileName = Path.GetFileName(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(safeOriginalFileName).ToLowerInvariant()}";
        var relativeFolder = Path.Combine(options.Value.LocalRoot, applicationId.ToString("N"));
        var absoluteFolder = Path.Combine(environment.ContentRootPath, relativeFolder);

        Directory.CreateDirectory(absoluteFolder);

        var relativePath = Path.Combine(relativeFolder, storedFileName);
        var absolutePath = Path.Combine(absoluteFolder, storedFileName);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return ServiceResult<StoredDocumentFile>.Success(new StoredDocumentFile(storedFileName, relativePath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.CompletedTask;
        }

        var absolutePath = Path.Combine(environment.ContentRootPath, storagePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public Task<ServiceResult<StoredDocumentContent>> OpenReadAsync(
        string storagePath,
        string contentType,
        string downloadFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.FromResult(ServiceResult<StoredDocumentContent>.Failure("Document file path is missing."));
        }

        var absolutePath = Path.Combine(environment.ContentRootPath, storagePath);
        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(ServiceResult<StoredDocumentContent>.Failure("Document file was not found."));
        }

        var stream = File.OpenRead(absolutePath);
        return Task.FromResult(ServiceResult<StoredDocumentContent>.Success(new StoredDocumentContent(stream, contentType, downloadFileName)));
    }
}
