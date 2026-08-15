using System.Security.Claims;
using Lendora.Api.Common.Responses;
using Lendora.Application.Abstractions.Authentication;
using Lendora.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRegisterRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(CreateErrorResponse("Registration request is invalid.", StatusCodes.Status400BadRequest, validationErrors));
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return BadRequest(CreateErrorResponse("Registration failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result.Response, "Registration completed successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(CreateErrorResponse("Email and password are required.", StatusCodes.Status400BadRequest, ["Email and password are required."]));
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return BadRequest(CreateErrorResponse("Login failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result.Response, "Login completed successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(CreateErrorResponse("Refresh token is required.", StatusCodes.Status400BadRequest, ["Refresh token is required."]));
        }

        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return BadRequest(CreateErrorResponse("Refresh token request failed.", StatusCodes.Status400BadRequest, result.Errors));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result.Response, "Refresh token generated successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(CreateErrorResponse("Email is required.", StatusCodes.Status400BadRequest, ["Email is required."]));
        }

        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "If the email exists, a reset link has been generated.", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(CreateErrorResponse("The current user could not be resolved.", StatusCodes.Status401Unauthorized, ["A valid user identifier was not found in the access token."]));
        }

        var currentUser = await authService.GetCurrentUserAsync(userId, cancellationToken);
        if (currentUser is null)
        {
            return NotFound(CreateErrorResponse("The current user was not found.", StatusCodes.Status404NotFound, ["The user associated with this access token no longer exists."]));
        }

        return Ok(ApiResponse<CurrentUserDto>.Ok(currentUser, "Current user retrieved successfully.", HttpContext.TraceIdentifier));
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

    private static IReadOnlyCollection<string> ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }

        return errors;
    }
}
