namespace Lendora.Application.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
