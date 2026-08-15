using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class RepaymentScheduleItemConfiguration : IEntityTypeConfiguration<RepaymentScheduleItem>
{
    public void Configure(EntityTypeBuilder<RepaymentScheduleItem> builder)
    {
        builder.ToTable("RepaymentScheduleItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.OpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.ScheduledPayment)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.PrincipalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.InterestAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.ClosingBalance)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(item => item.LoanApplication)
            .WithMany(application => application.RepaymentScheduleItems)
            .HasForeignKey(item => item.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new { item.LoanApplicationId, item.InstallmentNumber })
            .IsUnique();
    }
}
