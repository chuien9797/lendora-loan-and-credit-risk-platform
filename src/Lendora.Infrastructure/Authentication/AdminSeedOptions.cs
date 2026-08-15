namespace Lendora.Infrastructure.Authentication;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string FullName { get; set; } = "Lendora Admin";
    public string Email { get; set; } = "admin@lendora.local";
    public string Password { get; set; } = "ChangeMe123!";
}
