using System.Security.Claims;
using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Admin;
using Lendora.Application.Admin;
using Lendora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(IAdminUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AdminUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await userManagementService.GetUsersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<AdminUserDto>>.Ok(users, "User accounts retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userManagementService.CreateUserAsync(request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("User account creation failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AdminUserDto>.Ok(result.Data, "User account created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userManagementService.UpdateUserAsync(id, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(CreateErrorResponse("User account update failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AdminUserDto>.Ok(result.Data, "User account updated successfully.", HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var currentAdminId = GetRequiredUserId();
        if (currentAdminId is null)
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var result = await userManagementService.DeleteUserAsync(id, currentAdminId.Value, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(CreateErrorResponse("User account deletion failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "User account deleted successfully.", HttpContext.TraceIdentifier));
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
