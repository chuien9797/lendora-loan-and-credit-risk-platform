using Lendora.Application.Abstractions.Authentication;
using Lendora.Application.Authentication;
using Lendora.Domain.Constants;
using Lendora.Infrastructure.Data;
using Lendora.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lendora.Infrastructure.Authentication;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            return AuthResult.Failure("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Failure(createResult.Errors.Select(error => error.Description).ToArray());
        }

        await userManager.AddToRoleAsync(user, ApplicationRoles.Customer);

        logger.LogInformation("Registered new customer account for {Email}", normalizedEmail);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return AuthResult.Failure("This account is disabled.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Failure("This account is locked.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await dbContext.RefreshTokens
            .OrderByDescending(token => token.CreatedAtUtc)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return AuthResult.Failure("Refresh token is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(refreshToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return AuthResult.Failure("The user associated with this refresh token is not available.");
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResultAsync(user, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !user.IsActive)
        {
            logger.LogInformation("Ignored password reset request for unavailable account {Email}", normalizedEmail);
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        logger.LogInformation("Generated password reset token for {Email}: {ResetToken}", normalizedEmail, resetToken);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.FullName, user.IsActive, roles.ToArray());
    }

    private async Task<AuthResult> CreateAuthResultAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var tokens = await tokenService.CreateTokensAsync(user, cancellationToken);
        var roles = await userManager.GetRolesAsync(user);

        var response = new AuthResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAtUtc,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAtUtc,
            new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.FullName, user.IsActive, roles.ToArray()));

        return AuthResult.Success(response);
    }
}
