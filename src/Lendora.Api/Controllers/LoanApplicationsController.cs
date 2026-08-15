using System.Security.Claims;
using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Affordability;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.BankChecks;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Abstractions.Loans;
using Lendora.Application.Abstractions.Repayments;
using Lendora.Application.Abstractions.Risk;
using Lendora.Application.Affordability;
using Lendora.Application.Audit;
using Lendora.Application.BankChecks;
using Lendora.Application.Documents;
using Lendora.Application.Loans;
using Lendora.Application.Repayments;
using Lendora.Application.Risk;
using Lendora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/loan-applications")]
public sealed class LoanApplicationsController(
    ILoanApplicationService loanApplicationService,
    IAutomatedBankCheckService automatedBankCheckService,
    IAffordabilityAssessmentService affordabilityAssessmentService,
    IRiskAssessmentService riskAssessmentService,
    IRepaymentScheduleService repaymentScheduleService,
    IDocumentMetadataService documentMetadataService,
    IApplicationAuditService applicationAuditService,
    IDocumentStorageService documentStorageService) : ControllerBase
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

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDraft(CreateLoanApplicationRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.CreateDraftAsync(customerId.Value, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Draft application creation failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Draft loan application created successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDraft(Guid id, UpdateLoanApplicationRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.UpdateDraftAsync(customerId.Value, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Draft application update failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Draft loan application updated successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.SubmitAsync(customerId.Value, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan application submission failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        var bankCheckResult = await automatedBankCheckService.RunAsync(id, reviewedByUserId: null, cancellationToken);
        if (!bankCheckResult.Succeeded)
        {
            return BadRequest(CreateErrorResponse("Automated bank checks failed after submission.", StatusCodes.Status400BadRequest, bankCheckResult.Errors));
        }

        var refreshedResult = await loanApplicationService.GetApplicationAsync(customerId.Value, [ApplicationRoles.Customer], id, cancellationToken);
        if (!refreshedResult.Succeeded || refreshedResult.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan application lookup failed after automated bank checks.", StatusCodes.Status400BadRequest, refreshedResult.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(refreshedResult.Data, "Loan application submitted and automated bank checks completed successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPost("{id:guid}/accept-offer")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptOffer(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.AcceptOfferAsync(customerId.Value, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan offer acceptance failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Loan offer accepted successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDraft(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.DeleteDraftAsync(customerId.Value, id, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(CreateErrorResponse("Draft application deletion failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Draft loan application deleted successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApplications(CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var applications = await loanApplicationService.GetMyApplicationsAsync(customerId.Value, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>.Ok(applications, "Loan applications retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await loanApplicationService.GetApplicationAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Loan application lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Loan application retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPost("{id:guid}/bank-checks/run")]
    [ProducesResponseType(typeof(ApiResponse<AutomatedBankCheckDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunAutomatedBankChecks(Guid id, CancellationToken cancellationToken)
    {
        var staffUserId = GetRequiredUserId();
        if (staffUserId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await automatedBankCheckService.RunAsync(id, staffUserId.Value, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Automated bank checks failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AutomatedBankCheckDto>.Ok(result.Data, "Automated bank checks completed successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpGet("review-queue")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewQueue(CancellationToken cancellationToken)
    {
        var applications = await loanApplicationService.GetReviewQueueAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>.Ok(applications, "Review queue retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchApplications([FromQuery] string? query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>.Ok([], "Enter at least 2 characters to search applications.", HttpContext.TraceIdentifier));
        }

        var applications = await loanApplicationService.SearchApplicationsAsync(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<LoanApplicationSummaryDto>>.Ok(applications, "Application search completed successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPatch("{id:guid}/bank-review")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBankReview(Guid id, UpdateBankReviewRequest request, CancellationToken cancellationToken)
    {
        var staffUserId = GetRequiredUserId();
        if (staffUserId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await loanApplicationService.UpdateBankReviewAsync(staffUserId.Value, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Bank review update failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Bank review updated successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPatch("{id:guid}/decision")]
    [ProducesResponseType(typeof(ApiResponse<LoanApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDecision(Guid id, UpdateApplicationDecisionRequest request, CancellationToken cancellationToken)
    {
        var staffUserId = GetRequiredUserId();
        if (staffUserId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await loanApplicationService.UpdateDecisionAsync(staffUserId.Value, roles, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Application decision update failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<LoanApplicationDto>.Ok(result.Data, "Application decision updated successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpGet("{id:guid}/affordability-assessment")]
    [ProducesResponseType(typeof(ApiResponse<AffordabilityAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAffordabilityAssessment(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await affordabilityAssessmentService.GetAssessmentAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Affordability assessment lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AffordabilityAssessmentDto>.Ok(result.Data, "Affordability assessment retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPost("{id:guid}/affordability-assessment")]
    [ProducesResponseType(typeof(ApiResponse<AffordabilityAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAffordabilityAssessment(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await affordabilityAssessmentService.GenerateAssessmentAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Affordability assessment failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AffordabilityAssessmentDto>.Ok(result.Data, "Affordability assessment generated successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpGet("{id:guid}/risk-assessment")]
    [ProducesResponseType(typeof(ApiResponse<RiskAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRiskAssessment(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await riskAssessmentService.GetAssessmentAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Risk assessment lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<RiskAssessmentDto>.Ok(result.Data, "Risk assessment retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPost("{id:guid}/risk-assessment")]
    [ProducesResponseType(typeof(ApiResponse<RiskAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateRiskAssessment(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await riskAssessmentService.GenerateAssessmentAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Risk assessment failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<RiskAssessmentDto>.Ok(result.Data, "Risk assessment generated successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/repayment-schedule")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<RepaymentScheduleItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRepaymentSchedule(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await repaymentScheduleService.GetScheduleAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Repayment schedule lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<IReadOnlyCollection<RepaymentScheduleItemDto>>.Ok(result.Data, "Repayment schedule retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPost("{id:guid}/documents/metadata")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddDocumentMetadata(Guid id, CreateDocumentMetadataRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await documentMetadataService.AddMetadataAsync(customerId.Value, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document metadata creation failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<ApplicationDocumentDto>.Ok(result.Data, "Document metadata added successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.Customer)]
    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] int documentType, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var customerId = GetRequiredUserId();
        if (customerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var validationErrors = ValidateUploadedFile(file);
        if (!Enum.IsDefined(typeof(Lendora.Domain.Enums.ApplicationDocumentType), documentType))
        {
            validationErrors.Add("Document type is invalid.");
        }

        if (validationErrors.Count > 0)
        {
            return BadRequest(CreateErrorResponse("Document upload failed.", StatusCodes.Status400BadRequest, validationErrors));
        }

        var safeOriginalFileName = Path.GetFileName(file.FileName);
        await using var uploadStream = file.OpenReadStream();
        var storedFile = await documentStorageService.SaveAsync(
            id,
            safeOriginalFileName,
            file.ContentType,
            uploadStream,
            cancellationToken);
        if (!storedFile.Succeeded || storedFile.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document upload failed.", StatusCodes.Status400BadRequest, storedFile.Errors));
        }

        var request = new CreateDocumentMetadataRequest(
            (Lendora.Domain.Enums.ApplicationDocumentType)documentType,
            safeOriginalFileName,
            file.Length,
            file.ContentType,
            storedFile.Data.StoredFileName,
            storedFile.Data.StoragePath);

        var result = await documentMetadataService.AddMetadataAsync(customerId.Value, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            await documentStorageService.DeleteAsync(storedFile.Data.StoragePath, cancellationToken);
            return BadRequest(CreateErrorResponse("Document metadata creation failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<ApplicationDocumentDto>.Ok(result.Data, "Document uploaded successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/documents")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ApplicationDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await documentMetadataService.GetDocumentsAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document metadata lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<IReadOnlyCollection<ApplicationDocumentDto>>.Ok(result.Data, "Document metadata retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpGet("{id:guid}/audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ApplicationAuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuditLogs(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var result = await applicationAuditService.GetForApplicationAsync(userId.Value, roles, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Application audit trail lookup failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<IReadOnlyCollection<ApplicationAuditLogDto>>.Ok(result.Data, "Application audit trail retrieved successfully.", HttpContext.TraceIdentifier));
    }

    private Guid? GetRequiredUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private ApiErrorResponse CreateErrorResponse(string message, int statusCode, IReadOnlyCollection<string> errors) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            TraceId = HttpContext.TraceIdentifier,
            Errors = errors
        };

    private static List<string> ValidateUploadedFile(IFormFile? file)
    {
        var errors = new List<string>();

        if (file is null || file.Length == 0)
        {
            errors.Add("A document file is required.");
            return errors;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            errors.Add("Each document must be 10 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errors.Add("Document file extension must be pdf, jpg, jpeg, png, tif, or tiff.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            errors.Add("Document must be a PDF, JPG, PNG, or TIFF file.");
        }

        return errors;
    }
}
