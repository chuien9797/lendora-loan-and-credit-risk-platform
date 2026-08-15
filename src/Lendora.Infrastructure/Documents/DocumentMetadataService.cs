using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Documents;
using Lendora.Application.Loans;
using Lendora.Domain.Constants;
using Lendora.Domain.Entities;
using Lendora.Domain.Enums;
using Lendora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Documents;

internal sealed class DocumentMetadataService(ApplicationDbContext dbContext, IApplicationAuditService auditService) : IDocumentMetadataService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff"
    };

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff"
    };

    public async Task<ServiceResult<ApplicationDocumentDto>> AddMetadataAsync(Guid userId, Guid applicationId, CreateDocumentMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId && candidate.CustomerId == userId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("Loan application not found.");
        }

        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure(validationErrors.ToArray());
        }

        var document = new ApplicationDocument
        {
            LoanApplicationId = applicationId,
            DocumentType = request.DocumentType,
            OriginalFileName = request.OriginalFileName.Trim(),
            StoredFileName = string.IsNullOrWhiteSpace(request.StoredFileName) ? null : request.StoredFileName.Trim(),
            StoragePath = string.IsNullOrWhiteSpace(request.StoragePath) ? null : request.StoragePath.Trim(),
            FileSize = request.FileSize,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            UploadedByUserId = userId,
            UploadedAtUtc = DateTime.UtcNow,
            SubmittedToBank = application.Status != LoanApplicationStatus.Draft,
            Status = ApplicationDocumentStatus.PendingReview
        };

        dbContext.ApplicationDocuments.Add(document);
        await auditService.RecordAsync(
            applicationId,
            userId,
            ApplicationRoles.Customer,
            "DocumentUploaded",
            "Customer uploaded an application document.",
            $"{document.DocumentType}: {document.OriginalFileName} ({document.FileSize} bytes).",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
    }

    public async Task<ServiceResult<IReadOnlyCollection<ApplicationDocumentDto>>> GetDocumentsAsync(Guid userId, IReadOnlyCollection<string> roles, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ServiceResult<IReadOnlyCollection<ApplicationDocumentDto>>.Failure("Loan application not found.");
        }

        if (!CanAccessApplication(userId, roles, application))
        {
            return ServiceResult<IReadOnlyCollection<ApplicationDocumentDto>>.Failure("You do not have access to this loan application.");
        }

        var isStaff = IsStaff(roles);
        var documents = await dbContext.ApplicationDocuments
            .AsNoTracking()
            .Where(document => document.LoanApplicationId == applicationId)
            .Where(document => !isStaff || document.SubmittedToBank)
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(document => MapToDto(document))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<ApplicationDocumentDto>>.Success(documents);
    }

    public async Task<ServiceResult<ApplicationDocumentDto>> GetDocumentAsync(Guid userId, IReadOnlyCollection<string> roles, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.ApplicationDocuments
            .AsNoTracking()
            .Include(candidate => candidate.LoanApplication)
            .FirstOrDefaultAsync(candidate => candidate.Id == documentId, cancellationToken);

        if (document?.LoanApplication is null)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("Document metadata not found.");
        }

        if (!CanAccessApplication(userId, roles, document.LoanApplication))
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("You do not have access to this document.");
        }

        if (IsStaff(roles) && !document.SubmittedToBank)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("This document is still a customer draft attachment.");
        }

        return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
    }

    public async Task<ServiceResult<ApplicationDocumentDto>> ReviewAsync(Guid reviewerId, Guid documentId, ReviewDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.ApplicationDocuments
            .FirstOrDefaultAsync(candidate => candidate.Id == documentId, cancellationToken);

        if (document is null)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure("Document metadata not found.");
        }

        var validationErrors = ValidateReviewRequest(request);
        if (validationErrors.Count > 0)
        {
            return ServiceResult<ApplicationDocumentDto>.Failure(validationErrors.ToArray());
        }

        document.Status = request.Status;
        document.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim();
        document.ReviewedByUserId = reviewerId;
        document.ReviewedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await auditService.RecordAsync(
            document.LoanApplicationId,
            reviewerId,
            "Staff",
            "DocumentReviewed",
            $"Staff marked {document.DocumentType} as {document.Status}.",
            string.IsNullOrWhiteSpace(document.ReviewNote) ? null : $"Review note: {document.ReviewNote}",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<ApplicationDocumentDto>.Success(MapToDto(document));
    }

    private static bool CanAccessApplication(Guid userId, IReadOnlyCollection<string> roles, LoanApplication application)
    {
        return IsStaff(roles) || application.CustomerId == userId;
    }

    private static bool IsStaff(IReadOnlyCollection<string> roles) =>
        roles.Contains(ApplicationRoles.Admin) || roles.Contains(ApplicationRoles.LoanOfficer) || roles.Contains(ApplicationRoles.Underwriter);

    private static List<string> ValidateCreateRequest(CreateDocumentMetadataRequest request)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(request.DocumentType))
        {
            errors.Add("Document type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            errors.Add("Original file name is required.");
        }

        if (request.FileSize <= 0)
        {
            errors.Add("File size must be greater than 0.");
        }

        if (request.FileSize > MaxFileSizeBytes)
        {
            errors.Add("Each document must be 10 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            errors.Add("Content type is required.");
        }
        else if (!AllowedContentTypes.Contains(request.ContentType.Trim()))
        {
            errors.Add("Document must be a PDF, JPG, PNG, or TIFF file.");
        }

        var extension = Path.GetExtension(request.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errors.Add("Document file extension must be pdf, jpg, jpeg, png, tif, or tiff.");
        }

        return errors;
    }

    private static List<string> ValidateReviewRequest(ReviewDocumentRequest request)
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(request.Status) || request.Status == ApplicationDocumentStatus.PendingReview)
        {
            errors.Add("Review status must be accepted, rejected, or resubmission required.");
        }

        if ((request.Status == ApplicationDocumentStatus.Rejected || request.Status == ApplicationDocumentStatus.ResubmissionRequired) &&
            string.IsNullOrWhiteSpace(request.ReviewNote))
        {
            errors.Add("A review note is required when a document is rejected or needs resubmission.");
        }

        return errors;
    }

    private static ApplicationDocumentDto MapToDto(ApplicationDocument document) =>
        new(
            document.Id,
            document.LoanApplicationId,
            document.DocumentType,
            document.OriginalFileName,
            document.StoredFileName,
            document.StoragePath,
            document.FileSize,
            document.ContentType,
            document.UploadedByUserId,
            document.UploadedAtUtc,
            document.SubmittedToBank,
            document.Status,
            document.ReviewNote,
            document.ReviewedByUserId,
            document.ReviewedAtUtc,
            document.OcrStatus,
            document.OcrProvider,
            document.OcrConfidence,
            document.OcrSuggestedMonthlyIncome,
            document.OcrSuggestedMonthlyExpenses,
            document.OcrSuggestedNationalIdNumber,
            document.OcrNationalIdMatchesApplication,
            document.OcrSuggestedAddress,
            document.OcrDocumentDate,
            document.OcrIsRecent,
            document.OcrVerificationStatus,
            document.OcrVerificationFindings,
            document.OcrSummary,
            document.OcrExtractedText,
            document.OcrFailureReason,
            document.OcrProcessedByUserId,
            document.OcrProcessedAtUtc);
}
