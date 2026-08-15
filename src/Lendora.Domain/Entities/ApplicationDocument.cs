using Lendora.Domain.Enums;

namespace Lendora.Domain.Entities;

public class ApplicationDocument : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public ApplicationDocumentType DocumentType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? StoredFileName { get; set; }
    public string? StoragePath { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public bool SubmittedToBank { get; set; }
    public ApplicationDocumentStatus Status { get; set; } = ApplicationDocumentStatus.PendingReview;
    public string? ReviewNote { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DocumentOcrStatus OcrStatus { get; set; } = DocumentOcrStatus.NotStarted;
    public string? OcrProvider { get; set; }
    public decimal? OcrConfidence { get; set; }
    public decimal? OcrSuggestedMonthlyIncome { get; set; }
    public decimal? OcrSuggestedMonthlyExpenses { get; set; }
    public string? OcrSuggestedNationalIdNumber { get; set; }
    public bool? OcrNationalIdMatchesApplication { get; set; }
    public string? OcrSuggestedAddress { get; set; }
    public DateTime? OcrDocumentDate { get; set; }
    public bool? OcrIsRecent { get; set; }
    public string? OcrVerificationStatus { get; set; }
    public string? OcrVerificationFindings { get; set; }
    public string? OcrSummary { get; set; }
    public string? OcrExtractedText { get; set; }
    public string? OcrFailureReason { get; set; }
    public Guid? OcrProcessedByUserId { get; set; }
    public DateTime? OcrProcessedAtUtc { get; set; }

    public LoanApplication? LoanApplication { get; set; }
}
