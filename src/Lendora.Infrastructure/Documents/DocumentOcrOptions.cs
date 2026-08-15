namespace Lendora.Infrastructure.Documents;

internal sealed class DocumentOcrOptions
{
    public const string SectionName = "DocumentOcr";

    public string Provider { get; set; } = "Disabled";
    public string? UploadsBucket { get; set; }
}
