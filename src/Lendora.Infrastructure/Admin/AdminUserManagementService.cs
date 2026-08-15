using Lendora.Application.Abstractions.Admin;
using Lendora.Application.Admin;
using Lendora.Application.Loans;
using Lendora.Domain.Constants;
using Lendora.Infrastructure.Data;
using Lendora.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Infrastructure.Admin;

internal sealed class AdminUserManagementService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IAdminUserManagementService
{
    public async Task<IReadOnlyCollection<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var results = new List<AdminUserDto>();
        foreach (var user in users)
        {
            results.Add(await MapToDtoAsync(user));
        }

        return results;
    }

    public async Task<ServiceResult<AdminUserDto>> CreateUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var errors = ValidateUserRequest(request.FullName, request.Email, request.Roles, requireRole: true);
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters.");
        }

        if (errors.Count > 0)
        {
            return ServiceResult<AdminUserDto>.Failure(errors.ToArray());
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return ServiceResult<AdminUserDto>.Failure("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            EmailConfirmed = true,
            IsActive = request.IsActive
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<AdminUserDto>.Failure(createResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleResult = await userManager.AddToRolesAsync(user, NormalizeRoles(request.Roles));
        if (!roleResult.Succeeded)
        {
            return ServiceResult<AdminUserDto>.Failure(roleResult.Errors.Select(error => error.Description).ToArray());
        }

        return ServiceResult<AdminUserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<ServiceResult<AdminUserDto>> UpdateUserAsync(Guid id, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return ServiceResult<AdminUserDto>.Failure("User account not found.");
        }

        var errors = ValidateUserRequest(request.FullName, request.Email, request.Roles, requireRole: true);
        if (errors.Count > 0)
        {
            return ServiceResult<AdminUserDto>.Failure(errors.ToArray());
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingByEmail = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingByEmail is not null && existingByEmail.Id != user.Id)
        {
            return ServiceResult<AdminUserDto>.Failure("Another user already uses this email.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.IsActive = request.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ServiceResult<AdminUserDto>.Failure(updateResult.Errors.Select(error => error.Description).ToArray());
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var desiredRoles = NormalizeRoles(request.Roles);
        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles.Except(desiredRoles));
        if (!removeResult.Succeeded)
        {
            return ServiceResult<AdminUserDto>.Failure(removeResult.Errors.Select(error => error.Description).ToArray());
        }

        var addResult = await userManager.AddToRolesAsync(user, desiredRoles.Except(currentRoles));
        if (!addResult.Succeeded)
        {
            return ServiceResult<AdminUserDto>.Failure(addResult.Errors.Select(error => error.Description).ToArray());
        }

        return ServiceResult<AdminUserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id, Guid currentAdminId, CancellationToken cancellationToken = default)
    {
        if (id == currentAdminId)
        {
            return ServiceResult<bool>.Failure("Admins cannot delete their own account.");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return ServiceResult<bool>.Failure("User account not found.");
        }

        var hasApplications = await dbContext.LoanApplications
            .AnyAsync(application => application.CustomerId == id, cancellationToken);
        if (hasApplications)
        {
            return ServiceResult<bool>.Failure("This user has loan application history. Disable the account instead of deleting it.");
        }

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return ServiceResult<bool>.Failure(deleteResult.Errors.Select(error => error.Description).ToArray());
        }

        return ServiceResult<bool>.Success(true);
    }

    private static List<string> ValidateUserRequest(string fullName, string email, IReadOnlyCollection<string> roles, bool requireRole)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            errors.Add("A valid email is required.");
        }

        var normalizedRoles = NormalizeRoles(roles);
        if (requireRole && normalizedRoles.Count == 0)
        {
            errors.Add("At least one role is required.");
        }

        if (normalizedRoles.Count > 1)
        {
            errors.Add("Choose exactly one role for each account.");
        }

        if (normalizedRoles.Any(role => !ApplicationRoles.All.Contains(role)))
        {
            errors.Add("One or more selected roles are invalid.");
        }

        return errors;
    }

    private static IReadOnlyCollection<string> NormalizeRoles(IReadOnlyCollection<string> roles) =>
        roles
            .Select(role => ApplicationRoles.All.FirstOrDefault(known => string.Equals(known, role, StringComparison.OrdinalIgnoreCase)))
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Cast<string>()
            .Distinct()
            .ToArray();

    private async Task<AdminUserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AdminUserDto(user.Id, user.FullName, user.Email ?? string.Empty, user.IsActive, roles.Order().ToArray());
    }
}
