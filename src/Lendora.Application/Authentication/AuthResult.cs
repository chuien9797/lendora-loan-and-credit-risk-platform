namespace Lendora.Application.Authentication;

public sealed record AuthResult(
    bool Succeeded,
    AuthResponse? Response,
    IReadOnlyCollection<string> Errors)
{
    public static AuthResult Success(AuthResponse response) => new(true, response, []);

    public static AuthResult Failure(params string[] errors) => new(false, null, errors);
}
