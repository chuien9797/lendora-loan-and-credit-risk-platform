namespace Lendora.Domain.Entities;

public sealed class ApplicationAuditLog : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication? LoanApplication { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Details { get; set; }
}
