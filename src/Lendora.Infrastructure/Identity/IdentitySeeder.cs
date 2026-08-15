using Lendora.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lendora.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<Authentication.AdminSeedOptions> adminSeedOptions,
    ILogger<IdentitySeeder> logger)
{
    private readonly Authentication.AdminSeedOptions _adminSeedOptions = adminSeedOptions.Value;

    public async Task SeedAsync()
    {
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await EnsureUserInRoleAsync(_adminSeedOptions.FullName, _adminSeedOptions.Email, _adminSeedOptions.Password, ApplicationRoles.Admin);
        await EnsureUserInRoleAsync("Loan Officer", "officer@lendora.local", "Officer12345", ApplicationRoles.LoanOfficer);
        await EnsureUserInRoleAsync("Underwriter", "underwriter@lendora.local", "Underwriter12345", ApplicationRoles.Underwriter);
        await EnsureUserInRoleAsync("James Davies", "customer@lendora.local", "Customer12345", ApplicationRoles.Customer);
    }

    private async Task EnsureUserInRoleAsync(string fullName, string email, string password, string role)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail,
                UserName = normalizedEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to create seeded user {normalizedEmail}: {errors}");
            }

            logger.LogInformation("Created seeded {Role} account for {Email}", role, normalizedEmail);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
