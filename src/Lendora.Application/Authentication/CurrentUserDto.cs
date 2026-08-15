namespace Lendora.Application.Authentication;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
