namespace Lendora.Domain.Entities;

public sealed class RepaymentScheduleItem : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ScheduledPayment { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal ClosingBalance { get; set; }

    public LoanApplication? LoanApplication { get; set; }
}
