namespace Lendora.Application.Admin;

public sealed record CreateAdminUserRequest(
    string FullName,
    string Email,
    string Password,
    IReadOnlyCollection<string> Roles,
    bool IsActive = true);
