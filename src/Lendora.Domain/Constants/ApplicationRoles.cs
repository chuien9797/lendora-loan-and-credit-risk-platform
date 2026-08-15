namespace Lendora.Domain.Constants;

public static class ApplicationRoles
{
    public const string Customer = "Customer";
    public const string LoanOfficer = "LoanOfficer";
    public const string Underwriter = "Underwriter";
    public const string Admin = "Admin";

    public static readonly IReadOnlyCollection<string> All =
    [
        Customer,
        LoanOfficer,
        Underwriter,
        Admin
    ];
}
