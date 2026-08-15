using Lendora.Domain.Enums;

namespace Lendora.Domain.Entities;

public sealed class LoanProduct : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LoanProductType ProductType { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int MinTermMonths { get; set; }
    public int MaxTermMonths { get; set; }
    public decimal InterestRate { get; set; }
    public bool IsActive { get; set; } = true;
}
