using Amazon.Textract;
using Amazon.Textract.Model;
using Lendora.Application.Loans;
using Lendora.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Lendora.Infrastructure.Documents;

internal sealed class TextractDocumentTextExtractor(
    IAmazonTextract textractClient,
    IOptions<DocumentOcrOptions> options) : IDocumentTextExtractor
{
    public async Task<ServiceResult<ExtractedDocumentText>> ExtractAsync(
        ApplicationDocument document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.UploadsBucket))
        {
            return ServiceResult<ExtractedDocumentText>.Failure("Textract upload bucket is not configured.");
        }

        if (string.IsNullOrWhiteSpace(document.StoragePath))
        {
            return ServiceResult<ExtractedDocumentText>.Failure("Document has no stored file path for OCR.");
        }

        var request = new DetectDocumentTextRequest
        {
            Document = new Document
            {
                S3Object = new S3Object
                {
                    Bucket = options.Value.UploadsBucket,
                    Name = document.StoragePath
                }
            }
        };

        var response = await textractClient.DetectDocumentTextAsync(request, cancellationToken);
        var lineBlocks = response.Blocks
            .Where(block => block.BlockType == BlockType.LINE && !string.IsNullOrWhiteSpace(block.Text))
            .ToArray();

        var lines = lineBlocks
            .Select(block => block.Text.Trim())
            .Where(line => line.Length > 0)
            .ToArray();

        decimal? confidence = lineBlocks.Length == 0
            ? null
            : Math.Round((decimal)lineBlocks.Average(block => block.Confidence), 2);

        return ServiceResult<ExtractedDocumentText>.Success(new ExtractedDocumentText(lines, confidence));
    }
}
