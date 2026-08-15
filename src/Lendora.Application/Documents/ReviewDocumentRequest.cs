using Lendora.Domain.Enums;

namespace Lendora.Application.Documents;

public sealed record ReviewDocumentRequest(
    ApplicationDocumentStatus Status,
    string? ReviewNote);
