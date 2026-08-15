using Lendora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lendora.Infrastructure.Data.Configurations;

public sealed class RiskAssessmentConfiguration : IEntityTypeConfiguration<RiskAssessment>
{
    public void Configure(EntityTypeBuilder<RiskAssessment> builder)
    {
        builder.ToTable("RiskAssessments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Factors)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasOne(x => x.LoanApplication)
            .WithOne(x => x.RiskAssessment)
            .HasForeignKey<RiskAssessment>(x => x.LoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LoanApplicationId)
            .IsUnique();
    }
}
