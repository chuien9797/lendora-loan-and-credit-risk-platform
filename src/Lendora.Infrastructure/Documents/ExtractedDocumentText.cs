namespace Lendora.Infrastructure.Documents;

internal sealed record ExtractedDocumentText(
    IReadOnlyCollection<string> Lines,
    decimal? Confidence);
