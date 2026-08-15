using Lendora.Domain.Enums;

namespace Lendora.Application.Documents;

public sealed record CreateDocumentMetadataRequest(
    ApplicationDocumentType DocumentType,
    string OriginalFileName,
    long FileSize,
    string ContentType,
    string? StoredFileName = null,
    string? StoragePath = null);
