using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lendora.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<LoanProduct> LoanProducts { get; }
    DbSet<LoanApplication> LoanApplications { get; }
    DbSet<AffordabilityAssessment> AffordabilityAssessments { get; }
    DbSet<RiskAssessment> RiskAssessments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
