using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("LoanApplications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicantFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NationalIdNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.LoanPurpose)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.EmployerOrBusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EmployerOrBusinessRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.LoanAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MonthlyIncome)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MonthlyExpenses)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ExistingMonthlyDebt)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreditScoreSource)
            .HasMaxLength(100);

        builder.Property(x => x.CcrisRecordSummary)
            .HasMaxLength(1000);

        builder.Property(x => x.IncomeVerificationStatus)
            .HasMaxLength(100);

        builder.Property(x => x.RecommendedInitialLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ApprovedLimit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.LimitDecisionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.OfferedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DecisionNote)
            .HasMaxLength(1000);

        builder.HasOne(x => x.LoanProduct)
            .WithMany()
            .HasForeignKey(x => x.LoanProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
    }
}
