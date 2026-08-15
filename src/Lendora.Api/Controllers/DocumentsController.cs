using System.Security.Claims;
using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Documents;
using Lendora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(
    IDocumentMetadataService documentMetadataService,
    IDocumentOcrService documentOcrService,
    IDocumentStorageService documentStorageService) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        if (userId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var documentResult = await documentMetadataService.GetDocumentAsync(userId.Value, roles, id, cancellationToken);
        if (!documentResult.Succeeded || documentResult.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document lookup failed.", StatusCodes.Status400BadRequest, documentResult.Errors));
        }

        if (string.IsNullOrWhiteSpace(documentResult.Data.StoragePath))
        {
            return BadRequest(CreateErrorResponse("Document file is not available.", StatusCodes.Status400BadRequest, ["The document has metadata but no stored file path."]));
        }

        var fileResult = await documentStorageService.OpenReadAsync(
            documentResult.Data.StoragePath,
            documentResult.Data.ContentType,
            documentResult.Data.OriginalFileName,
            cancellationToken);
        if (!fileResult.Succeeded || fileResult.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document download failed.", StatusCodes.Status400BadRequest, fileResult.Errors));
        }

        return File(fileResult.Data.Content, fileResult.Data.ContentType, fileResult.Data.DownloadFileName);
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPatch("{id:guid}/review")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Review(Guid id, ReviewDocumentRequest request, CancellationToken cancellationToken)
    {
        var reviewerId = GetRequiredUserId();
        if (reviewerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await documentMetadataService.ReviewAsync(reviewerId.Value, id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document metadata review failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<ApplicationDocumentDto>.Ok(result.Data, "Document metadata reviewed successfully.", HttpContext.TraceIdentifier));
    }

    [Authorize(Roles = ApplicationRoles.LoanOfficer + "," + ApplicationRoles.Underwriter + "," + ApplicationRoles.Admin)]
    [HttpPost("{id:guid}/ocr")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractOcr(Guid id, CancellationToken cancellationToken)
    {
        var reviewerId = GetRequiredUserId();
        if (reviewerId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await documentOcrService.ExtractAsync(reviewerId.Value, id, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("Document OCR extraction failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<ApplicationDocumentDto>.Ok(result.Data, "Document OCR extraction completed.", HttpContext.TraceIdentifier));
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
}
