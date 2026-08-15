using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class AffordabilityAssessmentConfiguration : IEntityTypeConfiguration<AffordabilityAssessment>
{
    public void Configure(EntityTypeBuilder<AffordabilityAssessment> builder)
    {
        builder.ToTable("AffordabilityAssessments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MonthlyRepayment)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TotalRepayment)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TotalInterest)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DebtServiceRatio)
            .HasColumnType("decimal(9,2)");

        builder.Property(x => x.DisposableIncome)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.LoanApplication)
            .WithOne(x => x.AffordabilityAssessment)
            .HasForeignKey<AffordabilityAssessment>(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LoanApplicationId)
            .IsUnique();
    }
}
