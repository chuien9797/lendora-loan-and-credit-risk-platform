namespace Lendora.Infrastructure.Documents;

internal sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = Path.Combine(".appdata", "uploads");
    public string? S3Bucket { get; set; }
}
