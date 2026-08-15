using Amazon.S3;
using Amazon.S3.Model;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Loans;
using Microsoft.Extensions.Options;

namespace Lendora.Infrastructure.Documents;

internal sealed class S3DocumentStorageService(
    IAmazonS3 s3Client,
    IOptions<DocumentStorageOptions> options) : IDocumentStorageService
{
    public async Task<ServiceResult<StoredDocumentFile>> SaveAsync(
        Guid applicationId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.S3Bucket))
        {
            return ServiceResult<StoredDocumentFile>.Failure("S3 document bucket is not configured.");
        }

        var safeOriginalFileName = Path.GetFileName(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(safeOriginalFileName).ToLowerInvariant()}";
        var objectKey = $"applications/{applicationId:N}/{storedFileName}";

        var request = new PutObjectRequest
        {
            BucketName = options.Value.S3Bucket,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await s3Client.PutObjectAsync(request, cancellationToken);

        return ServiceResult<StoredDocumentFile>.Success(new StoredDocumentFile(storedFileName, objectKey));
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.S3Bucket) || string.IsNullOrWhiteSpace(storagePath))
        {
            return;
        }

        await s3Client.DeleteObjectAsync(options.Value.S3Bucket, storagePath, cancellationToken);
    }

    public async Task<ServiceResult<StoredDocumentContent>> OpenReadAsync(
        string storagePath,
        string contentType,
        string downloadFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.S3Bucket))
        {
            return ServiceResult<StoredDocumentContent>.Failure("S3 document bucket is not configured.");
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return ServiceResult<StoredDocumentContent>.Failure("Document file path is missing.");
        }

        var response = await s3Client.GetObjectAsync(options.Value.S3Bucket, storagePath, cancellationToken);
        return ServiceResult<StoredDocumentContent>.Success(new StoredDocumentContent(
            response.ResponseStream,
            string.IsNullOrWhiteSpace(response.Headers.ContentType) ? contentType : response.Headers.ContentType,
            downloadFileName));
    }
}
