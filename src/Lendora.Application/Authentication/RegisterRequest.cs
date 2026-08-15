namespace Lendora.Application.Authentication;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password);
