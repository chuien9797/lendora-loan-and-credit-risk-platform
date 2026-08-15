namespace Lendora.Application.Admin;

public sealed record UpdateAdminUserRequest(
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
